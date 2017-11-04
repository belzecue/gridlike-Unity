﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gridlike {
	
	[ExecuteInEditMode]
	[RequireComponent(typeof(Grid))]
	public abstract class GridListener : MonoBehaviour {
		
		[SerializeField] public Grid grid { get; private set; }

		void Reset() {
			ResetListener ();
		}
		public virtual void Awake() {
			ResetListener ();
		}

		public virtual void OnDestroy() {
			if (this.grid != null) {
				this.grid.RemoveListener (this);
			}
		} 

		public virtual void ResetListener() {
			if (this.grid == null) {
				Grid grid = GetComponent<Grid> ();

				if (grid == null) {
					Debug.LogWarning ("[Gridlike] No Grid on this game object");
				} else {
					this.grid = grid;

					grid.AddListener (this);
				}
			}
		}
			
		public abstract void OnSet(int x, int y, Tile tile);
		public virtual void OnSetState (int x, int y, Tile tile, float oldState1, float oldState2, float oldState3) {
			OnSet (x, y, tile);
		}
		public virtual void OnSetSubId (int x, int y, Tile tile, int oldSubId) {
			OnSet (x, y, tile);
		}

		public virtual void OnShowRegion(int regionX, int regionY) {
			if (Application.isPlaying) {
				StartCoroutine (_OnShowRegion (regionX, regionY));
			} else {		
				FiniteGrid region = grid.GetRegion (regionX, regionY);

				int startX = regionX * Grid.REGION_SIZE;
				int endX = (regionX + 1) * Grid.REGION_SIZE;
				int startY = regionY * Grid.REGION_SIZE;
				int endY = (regionY + 1) * Grid.REGION_SIZE;

				for (int i = startX; i < endX; i++) {
					for(int j = startY; j < endY; j++) {
						Tile tile = region.Get (i - startX, j - startY);

						if(tile != null) OnSet(i, j, tile);
					}
				}
			}
		}

		IEnumerator _OnShowRegion(int regionX, int regionY) {
			FiniteGrid region = grid.GetRegion (regionX, regionY);

			int startX = regionX * Grid.REGION_SIZE;
			int endX = (regionX + 1) * Grid.REGION_SIZE;
			int startY = regionY * Grid.REGION_SIZE;
			int endY = (regionY + 1) * Grid.REGION_SIZE;

			for (int i = startX; i < endX; i++) {
				for(int j = startY; j < endY; j++) {
					Tile tile = region.Get (i - startX, j - startY);

					if(tile != null) OnSet(i, j, tile);
				}

				if (i % 3 == 0) yield return null;
			}
		}

		public abstract void OnHideRegion(int X, int Y);
	}
}