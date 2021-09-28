using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Node.
/// </summary>
namespace Algorithm {
	public class Node {

		/// <summary>
		/// The connections (neighbors).
		/// </summary>
		[SerializeField]
		protected List<Node> m_Connections = new List<Node>();
		public string node_name;
		public Vector3 node_position;

		public Node(Vector3 pos) {
			this.node_name = pos.ToString();
			this.node_position = pos;
        }

		/// <summary>
		/// Gets the connections (neighbors).
		/// </summary>
		/// <value>The connections.</value>
		public virtual List<Node> connections {
			get {
				return m_Connections;
			}
		}

		public Node this[int index] {
			get {
				return m_Connections[index];
			}
		}
	}

	/// <summary>
	/// The Path.
	/// </summary>
	public class Path {

		/// <summary>
		/// The nodes.
		/// </summary>
		protected List<Node> m_Nodes = new List<Node>();

		/// <summary>
		/// The length of the path.
		/// </summary>
		protected float m_Length = 0f;

		/// <summary>
		/// Gets the nodes.
		/// </summary>
		/// <value>The nodes.</value>
		public virtual List<Node> nodes {
			get {
				return m_Nodes;
			}
		}

		/// <summary>
		/// Gets the length of the path.
		/// </summary>
		/// <value>The length.</value>
		public virtual float length {
			get {
				return m_Length;
			}
		}

		/// <summary>
		/// Bake the path.
		/// Making the path ready for usage, Such as caculating the length.
		/// </summary>
		public virtual void Bake() {
			List<Node> calculated = new List<Node>();
			m_Length = 0f;
			for (int i = 0; i < m_Nodes.Count; i++) {
				Node node = m_Nodes[i];
				for (int j = 0; j < node.connections.Count; j++) {
					Node connection = node.connections[j];

					// Don't calcualte calculated nodes
					if (m_Nodes.Contains(connection) && !calculated.Contains(connection)) {

						// Calculating the distance between a node and connection when they are both available in path nodes list
						m_Length += Vector3.Distance(node.node_position, connection.node_position);
					}
				}
				calculated.Add(node);
			}
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString() {
			return string.Format(
				"Nodes: {0}\nLength: {1}",
				string.Join(
					", ",
					nodes.Select(node => node.node_name).ToArray()),
				length);
		}


		public int GetPathDirectionAtIndex(int index) {
			if (index >= this.nodes.Count - 1) {
				throw new ArgumentException("Path Index is out of range");
			}

			Vector3 currentPos = this.nodes[index].node_position;
			Vector3 nextPos = this.nodes[index+1].node_position;

			if(currentPos.x == nextPos.x) {
				if (currentPos.z > nextPos.z) {
					return 2;
                } else {
					return 0;
                }
            } else {
				if (currentPos.x > nextPos.x) {
					return -1;
				} else {
					return 1;
				}
			}
		}
	}

	/// <summary>
	/// The Graph.
	/// </summary>
	public class Graph {

		/// <summary>
		/// The nodes.
		/// </summary>
		[SerializeField]
		protected List<Node> m_Nodes = new List<Node>();
		public List<Path> m_Paths = new List<Path>();
		public Dictionary<string, Path> point2path = new Dictionary<string, Path>();

		public Graph() {
			m_Nodes = new List<Node>();
			m_Paths = new List<Path>();
			point2path = new Dictionary<string, Path>();
		}

		/// <summary>
		/// Gets the nodes.
		/// </summary>
		/// <value>The nodes.</value>
		public virtual List<Node> nodes {
			get {
				return m_Nodes;
			}
		}

		/// <summary>
		/// Gets the shortest path from the starting Node to the ending Node.
		/// </summary>
		/// <returns>The shortest path.</returns>
		/// <param name="start">Start Node.</param>
		/// <param name="end">End Node.</param>
		public void GetShortestPath(Node start, Node end) {
			m_Paths.Clear();

			// We don't accept null arguments
			if (start == null || end == null) {
				throw new ArgumentNullException();
			}

			// The final path
			Path path = new Path();

			// If the start and end are same node, we can return the start node
			if (start == end) {
				path.nodes.Add(start);
				m_Paths.Add(path);
			}

			string dictPointName = start.node_name.ToString() + ";" + end.node_name.ToString();

			if (point2path.ContainsKey(dictPointName)) {
				m_Paths.Add(point2path[dictPointName]);
				return;
            }

			// The list of unvisited nodes
			List<Node> unvisited = new List<Node>();

			// Previous nodes in optimal path from source
			Dictionary<Node, Node> previous = new Dictionary<Node, Node>();

			// The calculated distances, set all to Infinity at start, except the start Node
			Dictionary<Node, float> distances = new Dictionary<Node, float>();

			for (int i = 0; i < m_Nodes.Count; i++) {
				Node node = m_Nodes[i];
				unvisited.Add(node);

				// Setting the node distance to Infinity
				distances.Add(node, float.MaxValue);
			}

			// Set the starting Node distance to zero
			distances[start] = 0f;
			while (unvisited.Count != 0) {

				// Ordering the unvisited list by distance, smallest distance at start and largest at end
				unvisited = unvisited.OrderBy(node => distances[node]).ToList();

				// Getting the Node with smallest distance
				Node current = unvisited[0];

				// Remove the current node from unvisisted list
				unvisited.Remove(current);

				// When the current node is equal to the end node, then we can break and return the path
				if (current == end) {

					// Construct the shortest path
					while (previous.ContainsKey(current)) {

						// Insert the node onto the final result
						path.nodes.Insert(0, current);

						// Traverse from start to end
						current = previous[current];
					}

					// Insert the source onto the final result
					path.nodes.Insert(0, current);
					break;
				}

				// Looping through the Node connections (neighbors) and where the connection (neighbor) is available at unvisited list
				for (int i = 0; i < current.connections.Count; i++) {
					Node neighbor = current.connections[i];

					// Getting the distance between the current node and the connection (neighbor)
					float length = Vector3.Distance(current.node_position, neighbor.node_position);

					// The distance from start node to this connection (neighbor) of current node
					float alt = distances[current] + length;

					// A shorter path to the connection (neighbor) has been found
					if (alt < distances[neighbor]) {
						distances[neighbor] = alt;
						previous[neighbor] = current;
					}
				}
			}
			path.Bake();

			point2path.Add(dictPointName, path);
			m_Paths.Add(path);
		}

		public Node GetNearestNode(Vector3 targetPoint) {
			float minDist = 100f;
			Node nearestNode = null;
			foreach(var node in m_Nodes) {
				float nodeDist = Vector3.Distance(node.node_position, targetPoint);
				if (nodeDist < minDist) {
					minDist = nodeDist;
					nearestNode = node;
                }
            }
			return nearestNode;
        }

		public int CalculateZigZagInPath(Path path) {
			int zigzag = 0;
			int x_or_z = 0;
			for(int i = 0; i < path.nodes.Count - 1; ++i) {
				if (path.nodes[i+1].node_position.x == path.nodes[i].node_position.x) {
					int current_zigzag = 1;
					if (current_zigzag != x_or_z) {
						zigzag++;
						x_or_z = current_zigzag;
                    }
                }

				if (path.nodes[i + 1].node_position.z == path.nodes[i].node_position.z) {
					int current_zigzag = -1;
					if (current_zigzag != x_or_z) {
						zigzag++;
						x_or_z = current_zigzag;
					}
				}
			}

			return zigzag;
        }
	}
}