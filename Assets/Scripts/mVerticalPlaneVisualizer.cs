//-----------------------------------------------------------------------
// <copyright file="DetectedPlaneVisualizer.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.Examples.Common
{
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
	using UnityEngine.UI;

    /// <summary>
    /// Visualizes a single DetectedPlane in the Unity scene.
    /// </summary>
    public class mVerticalPlaneVisualizer : MonoBehaviour
    {
        private static int s_PlaneCount = 0;

        private MeshCollider m_MeshCollider;

        private readonly Color[] k_PlaneColors = new Color[]
        {
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.956f, 0.262f, 0.211f),
            new Color(0.913f, 0.117f, 0.388f),
            new Color(0.611f, 0.152f, 0.654f),
            new Color(0.403f, 0.227f, 0.717f),
            new Color(0.247f, 0.317f, 0.709f),
            new Color(0.129f, 0.588f, 0.952f),
            new Color(0.011f, 0.662f, 0.956f),
            new Color(0f, 0.737f, 0.831f),
            new Color(0f, 0.588f, 0.533f),
            new Color(0.298f, 0.686f, 0.313f),
            new Color(0.545f, 0.764f, 0.290f),
            new Color(0.803f, 0.862f, 0.223f),
            new Color(1.0f, 0.921f, 0.231f),
            new Color(1.0f, 0.756f, 0.027f)
        };

        private DetectedPlane m_DetectedPlane;

        // Keep previous frame's mesh polygon to avoid mesh update every frame.
        private List<Vector3> m_PreviousFrameMeshVertices = new List<Vector3>();
        private List<Vector3> m_MeshVertices = new List<Vector3>();
        private List<Vector3> m_CollisionVerts = new List<Vector3>();


        private Vector3 m_PlaneCenter = new Vector3();
		private Vector3 m_PlaneNormal = Vector3.zero;

        private List<Color> m_MeshColors = new List<Color>();

        private List<int> m_MeshIndices = new List<int>();
		private List<int> m_CollisionIndices = new List<int>();


		private Mesh m_Mesh;
		private Mesh m_CollisionMesh;


		private MeshRenderer m_MeshRenderer;

        private float collisionTolerance = 0.08f;

		private Vector3 collisionBoundRight = Vector3.zero;
		private Vector3 collisionBoundLeft = Vector3.zero;

		private bool collisionOnRight = false;
		private bool collisionOnLeft = false;

		private RaycastHit[] hits;

		private Button m_ClipButton;

		private bool shouldClip = true;

		private int m_id = -1;



		//private float[] max_bounds = { float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity,
		//                               float.PositiveInfinity};


		/// <summary>
		/// The Unity Awake() method.
		/// </summary>
		public void Awake()
        {
            m_Mesh = GetComponent<MeshFilter>().mesh;
            m_MeshRenderer = GetComponent<UnityEngine.MeshRenderer>();

            m_MeshCollider = GetComponent<MeshCollider>();
            m_MeshCollider.sharedMesh = m_Mesh;

			GameObject clip = GameObject.Find("ClipButton");
			if (clip != null)
			{
				m_ClipButton = clip.GetComponent<Button>();
				m_ClipButton.onClick.AddListener(_handleToggleClip);
			}

		}

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (m_DetectedPlane == null)
            {
                return;
            }
            else if (m_DetectedPlane.SubsumedBy != null)
            {
                Destroy(gameObject);
                return;
            }
            else if (m_DetectedPlane.TrackingState != TrackingState.Tracking)
            {
                 m_MeshRenderer.enabled = false;
                 return;
            }

            m_MeshRenderer.enabled = true;

            _UpdateMeshIfNeeded();
        }

        /// <summary>
        /// Initializes the DetectedPlaneVisualizer with a DetectedPlane.
        /// </summary>
        /// <param name="plane">The plane to vizualize.</param>
        public void Initialize(DetectedPlane plane, int id)
        {
			m_id = id;

            m_DetectedPlane = plane;
				
			Color planeColor = (k_PlaneColors[s_PlaneCount++ % k_PlaneColors.Length]);

			m_MeshRenderer.material.SetColor("_GridColor", planeColor);


			Debug.Log("NEW PLANE! ID: " + m_id.ToString() + " COLOR: " + planeColor);


			m_MeshRenderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

            Update();
        }

        /// <summary>
        /// Update mesh with a list of Vector3 and plane's center position.
        /// </summary>
        private void _UpdateMeshIfNeeded(bool forceUpdate = false)
        {
            m_DetectedPlane.GetBoundaryPolygon(m_MeshVertices);

			if (!forceUpdate)
			{
				if (_AreVerticesListsEqual(m_PreviousFrameMeshVertices, m_MeshVertices))
				{
					return;
				}
			}


            m_PreviousFrameMeshVertices.Clear();
            m_PreviousFrameMeshVertices.AddRange(m_MeshVertices);

            m_PlaneCenter = m_DetectedPlane.CenterPose.position;

            Vector3 planeNormal = m_DetectedPlane.CenterPose.rotation * Vector3.up;

			m_PlaneNormal = planeNormal;

            m_MeshRenderer.material.SetVector("_PlaneNormal", planeNormal);

            Vector3 planeRight = Vector3.Cross(Vector3.down, planeNormal);

			planeRight.Normalize();


            float maxY = float.NegativeInfinity;
            float minY = float.PositiveInfinity;

            float maxDot = float.NegativeInfinity;
            Vector3 maxRight = Vector3.zero;
            Vector3 colMaxRight = Vector3.zero;

            float minDot = float.PositiveInfinity;
            Vector3 minRight = Vector3.zero;
            Vector3 colMinRight = Vector3.zero;


            float dot;





            // finding bounding box
            for (int i = 0; i < m_MeshVertices.Count; i++)
            {
                minY = Mathf.Min(minY, m_MeshVertices[i].y);
                maxY = Mathf.Max(maxY, m_MeshVertices[i].y);

                dot = Vector3.Dot(planeRight, m_MeshVertices[i] - m_PlaneCenter);

                if (dot > maxDot)
                {
                    maxDot = dot;
                    maxRight = m_MeshVertices[i];
                }
                if (dot < minDot) 
                {
                    minDot = dot;
                    minRight = m_MeshVertices[i];
                }

            }

			colMaxRight = ((maxRight - m_PlaneCenter).normalized * (Mathf.Abs(maxDot) + collisionTolerance)) + m_PlaneCenter;
			colMinRight = ((minRight - m_PlaneCenter).normalized * (Mathf.Abs(minDot) + collisionTolerance)) + m_PlaneCenter;


			if (!collisionOnRight)
			{
				hits = Physics.RaycastAll(m_PlaneCenter, planeRight, 100.0F);
				Debug.DrawRay(m_PlaneCenter, planeRight * 100f, Color.red, 1000, false);

				for (int i = 0; i < hits.Length; i++)
				{
					RaycastHit hit = hits[i];

					if (hit.transform.gameObject != this.gameObject)
					{
						if (hit.transform.gameObject.name == "mVerticalPlaneVisualizer(Clone)")
						{

							mVerticalPlaneVisualizer other = (mVerticalPlaneVisualizer)hit.transform.gameObject.GetComponent(typeof(mVerticalPlaneVisualizer));

							Vector3 intersect = hitPointOnPlaneIntersect(m_PlaneCenter, m_PlaneNormal, other.m_PlaneCenter, other.m_PlaneNormal, hit.point);

							if (intersect != Vector3.zero)
							{
								float intersect_distance = PointLineDistance(hit.point, Vector3.up, maxRight);


								if (intersect_distance < collisionTolerance)
								{
									Debug.Log("Right intersect Distance: " + intersect_distance.ToString());


									collisionBoundRight = hit.point;

									collisionOnRight = true;
									Debug.Log("HIT RIGHT " + m_id.ToString());
									other.handleIncomingRayCast(hit.point);

								}
							}
						}
					}
				}
			}

			if (!collisionOnLeft)
			{
				hits = Physics.RaycastAll(m_PlaneCenter, -planeRight, 100.0F);

				Debug.DrawRay(m_PlaneCenter, -planeRight * 100f, Color.magenta, 1000, false);


				for (int i = 0; i < hits.Length; i++)
				{
					RaycastHit hit = hits[i];

					if (hit.transform.gameObject != this.gameObject)
					{

						if (hit.transform.gameObject.name == "mVerticalPlaneVisualizer(Clone)")
						{

							mVerticalPlaneVisualizer other = (mVerticalPlaneVisualizer)hit.transform.gameObject.GetComponent(typeof(mVerticalPlaneVisualizer));
							Vector3 intersect = hitPointOnPlaneIntersect(m_PlaneCenter, m_PlaneNormal, other.m_PlaneCenter, other.m_PlaneNormal, hit.point);

							if (intersect != Vector3.zero)
							{
							
								float intersect_distance = PointLineDistance(hit.point, Vector3.up, minRight);


								if (intersect_distance < collisionTolerance)
								{
									Debug.Log("Left intersect Distance: " + intersect_distance.ToString());


									collisionBoundLeft = hit.point;
									collisionOnLeft = true;
									Debug.Log("HIT LEFT " + m_id.ToString());

									other.handleIncomingRayCast(hit.point);

								}
							}
						}
					}
				}
			}



			if (shouldClip)
			{
				if (collisionOnRight)
				{
					maxDot = Vector3.Dot(planeRight, collisionBoundRight - m_PlaneCenter);
					maxRight = ((planeRight).normalized * (Mathf.Abs(maxDot) )) + m_PlaneCenter;
				}

				if (collisionOnLeft)
				{
					minDot = Vector3.Dot(planeRight, collisionBoundLeft - m_PlaneCenter);
					minRight = ((-planeRight).normalized * (Mathf.Abs(minDot))) + m_PlaneCenter;
				}
			}




            //int planePolygonCount = m_MeshVertices.Count;

            // The following code converts a polygon to a mesh with two polygons, inner
            // polygon renders with 100% opacity and fade out to outter polygon with opacity 0%, as shown below.
            // The indices shown in the diagram are used in comments below.
            // _______________     0_______________1
            // |             |      |4___________5|
            // |             |      | |         | |
            // |             | =>   | |         | |
            // |             |      | |         | |
            // |             |      |7-----------6|
            // ---------------     3---------------2




            m_MeshVertices.Clear();
			m_CollisionVerts.Clear();
            m_MeshColors.Clear();


            //bottomLeft
            m_MeshVertices.Add(new Vector3(minRight.x, minY, minRight.z));
            m_CollisionVerts.Add(new Vector3(colMinRight.x, minY, colMinRight.z));

            //topLeft
            m_MeshVertices.Add(new Vector3(minRight.x, maxY, minRight.z));
			m_CollisionVerts.Add(new Vector3(colMinRight.x, maxY, colMinRight.z));

            //topRight
            m_MeshVertices.Add(new Vector3(maxRight.x, maxY, maxRight.z));
			m_CollisionVerts.Add(new Vector3(colMaxRight.x, maxY, colMaxRight.z));

            //bottomRight
            m_MeshVertices.Add(new Vector3(maxRight.x, minY, maxRight.z));
			m_CollisionVerts.Add(new Vector3(colMaxRight.x, minY, colMaxRight.z));
			


            m_MeshColors.Add(Color.red);
            m_MeshColors.Add(Color.green);
            m_MeshColors.Add(Color.blue);
            m_MeshColors.Add(Color.magenta);


            m_MeshIndices.Clear();

			m_MeshIndices.Add(0);
            m_MeshIndices.Add(1);
            m_MeshIndices.Add(2);
            m_MeshIndices.Add(2);
            m_MeshIndices.Add(3);
            m_MeshIndices.Add(0);

           
            m_Mesh.Clear();
            m_Mesh.SetVertices(m_MeshVertices);
            m_Mesh.SetIndices(m_MeshIndices.ToArray(), MeshTopology.Triangles, 0);
            m_Mesh.SetColors(m_MeshColors);

			m_CollisionIndices.Clear();

			m_CollisionIndices.Add(0);
			m_CollisionIndices.Add(1);
			m_CollisionIndices.Add(2);

			m_CollisionIndices.Add(2);
			m_CollisionIndices.Add(3);
			m_CollisionIndices.Add(0);

			m_CollisionIndices.Add(0);
			m_CollisionIndices.Add(2);
			m_CollisionIndices.Add(1);

			m_CollisionIndices.Add(0);
			m_CollisionIndices.Add(3);
			m_CollisionIndices.Add(2);

			Mesh m_CollisionMesh = new Mesh();
			m_CollisionMesh.SetVertices(m_CollisionVerts);
			m_CollisionMesh.SetIndices(m_CollisionIndices.ToArray(), MeshTopology.Triangles, 0);

			m_MeshCollider.sharedMesh = null;

			m_MeshCollider.sharedMesh = m_CollisionMesh;

			m_MeshCollider.enabled = true;

        }


        private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
        {
            if (firstList.Count != secondList.Count)
            {
                return false;
            }

            for (int i = 0; i < firstList.Count; i++)
            {
                if (firstList[i] != secondList[i])
                {
                    return false;
                }
            }

            return true;
        }

        public void handleIncomingRayCast(Vector3 hitPose)
        {
			m_PlaneCenter = m_DetectedPlane.CenterPose.position;

			Vector3 planeRight = Vector3.Cross(Vector3.down, m_PlaneNormal);

			Debug.DrawRay(hitPose, Vector3.up * 100, Color.yellow, 1000, false);

			//Debug.DrawRay(m_PlaneCenter, m_PlaneNormal * 100, Color.cyan, 1000, false);

			if ((Vector3.Dot(planeRight, hitPose - m_PlaneCenter) > 0) && !collisionOnRight)
			{
				collisionBoundRight = hitPose;
				collisionOnRight = true;
				Debug.Log("INCOMING HIT on RIGHT " + m_id );

				_UpdateMeshIfNeeded(true);
			}
			else if ((Vector3.Dot(planeRight, hitPose - m_PlaneCenter) < 0) && !collisionOnLeft)
			{ 
				collisionBoundLeft = hitPose;
				collisionOnLeft = true;
				Debug.Log("INCOMING HIT on LEFT " + m_id );

				_UpdateMeshIfNeeded(true);

			}


		}

		private void _handleToggleClip()
		{
			shouldClip = !shouldClip;
			Debug.Log("should clip?: " + shouldClip.ToString());
			_UpdateMeshIfNeeded(true);
		}


		private Vector3 hitPointOnPlaneIntersect(Vector3 p1_pose, Vector3 p1_normal, Vector3 p2_pose, Vector3 p2_normal, Vector3 hit_pose)
		{
			Vector3 p3_normal = Vector3.Cross(p1_normal, p2_normal);
			float det = p3_normal.sqrMagnitude;

			// If the determinant is 0, that means parallel planes, no intersection.
			// note: you may want to check against an epsilon value here.
			if (det != 0.0f)
			{
				float p1_d = -1 * Vector3.Dot(p1_pose, p1_normal);
				float p2_d = -1 * Vector3.Dot(p2_pose, p2_normal);

				// calculate the final (point, normal)
				Vector3 r_pose = ((Vector3.Cross(p3_normal, p2_normal) * p1_d) +
						   (Vector3.Cross(p1_normal, p3_normal) * p2_d)) / det;

				Vector3 r_normal = p3_normal;

				//Debug.Log("Unity Hit Pose: " + hit_pose.ToString() + ",  Calculated hit pose: " + r_pose.ToString());

				Debug.DrawRay(hit_pose, Vector3.down * 100f, Color.blue, 1000, false);

				float distance = PointLineDistance(hit_pose, r_normal, r_pose);

				if (distance < 0.05f)
				{

					return r_pose;
				}
				else
				{
					return Vector3.zero;
				}

			}

			else
			{
				return Vector3.zero;
			}
		}

		private float PointLineDistance(Vector3 point, Vector3 line_d, Vector3 line_p)
		{
			return ((Vector3.Cross(line_d, line_p - point).magnitude) / line_d.magnitude);
		}
    }
}
