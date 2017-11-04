﻿using UnityEngine;
using System;
using UnityEditor;

namespace Gridlike {
	
	[Serializable]
	public class PlaceTool : GridTool {

		[SerializeField] int radius = 1;
		[SerializeField] int id = 1;

		public override bool UseWindow() {
			return true;
		}
		public override string Name() {
			return "place";
		}

		public override bool Window() {

			id = EditorGUILayout.IntField ("id", id);
			if (grid.atlas == null || id <= 0 || id >= grid.atlas.atlas.Length) id = 0;
			if (grid.atlas == null || grid.atlas [id] == null) id = 0;

			if (grid.atlas [id].tileGO == null) {
				radius = Mathf.Max(1, EditorGUILayout.IntField ("radius", radius));
			} else {
				radius = 1;
			}
			return false;
		}

		public override bool OnMouseDown() {
			Place ();
			return true;
		}
		public override bool OnMouseUp() {
			Place ();
			return true;
		}
		public override bool OnMouse() {
			Place ();
			return true;
		}

		void Place() {
			int x = mouseX, y = mouseY;

			bool hasPlaced = false;

			int r = radius - 1;

			for (int i = -r; i <= r; i++) {
				for (int j = -r; j <= r; j++) {
					if (grid.CanSet (x, y, id)) {
						hasPlaced = true;
						grid.Set (x + i, y + j, id, 0, 0, 0, 0);
					}
				}
			}

			if(hasPlaced) grid.PresentContainingRegion (mouseX, mouseY);
		}
	}
}