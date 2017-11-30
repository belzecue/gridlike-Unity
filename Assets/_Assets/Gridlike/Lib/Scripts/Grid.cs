﻿using System;
using System.Collections.Generic;
using UnityEngine;

// ## IMPROVEMENTS (for later)
// Grid updater component: iterates through tiles and updates there information

// Serialization: Allow the creation of the world ahead of time, saving to be loaded later on

// Scriptable objects storage of grid tiles

// Sub id calculation algorithm definition

// Depth based lighting

// # TICKET 11 - Make PC2D inherit speed from ship when loosing contact



// # TICKET 3 - Improve atlas (1day)
// Add the drag and drop if possible to the tile atlas (maybe into a window dedicated to it)
// Have sprite size verification with error boxes in tile atlas
// Tighter encapsulation with tile info and sprites

// # TICKET 4 - Grid data validation (1hour)
// Add some form of model validation including on a grid (runnable whenever):
// - does id exist
// - does subId exist
// - triangles are coherent
// - tileGO do not overlap

// Automatic fix (with options?)

// # TICKET 6 - Atlas is up to date (2hour)
// Concept of version id to signal a need for updating a grid (because the atlas changed since last version update)
// Push for update on the 
// The spritesheet doesn't update every time a sprite is added

// # TICKET 9 - Comment EVERYTHING + correct namespace (1day)
// comments on gridlike
// namespace on gridship

// TEST SCENARIOS
// Handle subids, see their limit, define contract and preconditions..
// Make sure if an id is said to be placeable at a given place, it is actually placeable there

// COMMENTS

// SAMPLES

// TUTORIALS

// MANUAL

// CLEAN UP
// Remove as many public field as possible (once tests are defined)

// FEATURE TO SHOWCASE
// 1. Atlas editing
// 2. Grid editing
// 3. Procedural generation
// 4. Simple ship + miner (place blocks wherever, mine whatever) example

namespace Gridlike {

	[ExecuteInEditMode]
	[AddComponentMenu("Gridlike/Grid")]
	[DisallowMultipleComponent]
	public class Grid : MovingPlatformMotor2D {

		public const int REGION_SIZE = 50;

		static List<Grid> grids;

		[SerializeField] GridDataDelegate gridDelegate;
		[SerializeField] List<GridListener> gridListeners;

		[SerializeField] InfiniteGrid tiles;
		[SerializeField] InfiniteTileGOGrid tileGOs;

		[HideInInspector] public TileAtlas atlas;

		// TODO add better indication to know what this means
		public bool useLoading;
		public bool useAgentBasedLoading;
		GridAgentLoadPolicy loadPolicy;

		public bool showOnSet;

		Dictionary<string, List<TileBehaviour>> nameToTileGO;
		List<Point> loadingTiles;

		void Init() {
			if (tiles == null) {
				gridDelegate = GetComponent<GridDataDelegate> ();
				gridListeners = new List<GridListener> ();

				tiles = new InfiniteGrid (REGION_SIZE);
				tileGOs = new InfiniteTileGOGrid (gameObject, REGION_SIZE);

				foreach (GridListener listener in GetComponents<GridListener> ()) {
					listener.ResetListener ();
				}
			}
		}

		#region UNITY EVENTS

		void Reset() {
			if(tiles != null) HideAll ();

			tiles = null;

			Init ();
		}
		void Awake() {
			if (grids == null) grids = new List<Grid> ();
			grids.Add (this);

			Init ();

			LoadExtra ();

			nameToTileGO = new Dictionary<string, List<TileBehaviour>> ();
			loadingTiles = new List<Point> ();

			foreach (FiniteGrid region in GetRegions()) {
				if (region.presented) {
					FiniteComponentGrid components = tileGOs.GetRegion (region.regionX, region.regionY);

					if(components != null) {

						for (int i = 0; i < Grid.REGION_SIZE; i++) {
							for (int j = 0; j < Grid.REGION_SIZE; j++) {
								Tile tile = region.Get (i, j);

								if (tile.tileGOCenter) {
									Component component = components.Get(i, j);

									if (component is TileBehaviour) {
										RegisterTileGO (tile, component as TileBehaviour);
									}
								}
							}
						}
					}
				}
			}
		}

		void OnDestroy() {
			foreach (GridListener listener in GetComponents<GridListener> ()) {
				listener.ResetListener ();
			}

			if (grids != null) grids.Remove (this);
		}

		void Update() {
			if (useLoading && useAgentBasedLoading) {
				if (loadPolicy == null) loadPolicy = new GridAgentLoadPolicy (this);

				loadPolicy.Update();
			}
		}

		#endregion 

		#region GRID DATA DELEGATE

		public void SetDelegate(GridDataDelegate gridDelegate) {
			this.gridDelegate = gridDelegate;
		}

		#endregion

		#region GRID LISTENERS

		public void AddListener(GridListener listener) {
			if (!gridListeners.Contains (listener)) {
				gridListeners.Add (listener);

				foreach (FiniteGrid region in tiles.GetRegions()) {
					if (region.presented) {
						listener.OnShowRegion (region.regionX, region.regionY);
					}
				}
			}
		}
		public void RemoveListener(GridListener listener) {
			if (gridListeners.Contains (listener)) {
				gridListeners.Remove (listener);
			}
		}

		#endregion

		#region REGION PRESENTING/LOADING

		public void PresentAllAgain() {
			List<FiniteGrid> regions = tiles.GetRegions ().FindAll(r => r.presented);
			foreach (FiniteGrid regionPosition in regions) {
				HideRegion (regionPosition.regionX, regionPosition.regionY);
			}

			foreach (FiniteGrid regionPosition in regions) {
				PresentRegion (regionPosition.regionX, regionPosition.regionY);
			}
		}

		public void PresentAll() {
			foreach (FiniteGrid region in tiles.GetRegions()) {
				PresentRegion (region.regionX, region.regionY);
			}
		}
		public void HideAll() {
			foreach (FiniteGrid region in tiles.GetRegions().FindAll(r => r.presented)) {
				HideRegion (region.regionX, region.regionY);
			}
		}

		public void PresentContainingRegion(int x, int y) {
			PresentRegion(Mathf.FloorToInt(x / (float) Grid.REGION_SIZE), Mathf.FloorToInt(y / (float) Grid.REGION_SIZE));
		}
		public void HideContainingRegion(int x, int y) {
			HideRegion(Mathf.FloorToInt(x / (float) Grid.REGION_SIZE), Mathf.FloorToInt(y / (float) Grid.REGION_SIZE));
		}

		public void PresentRegion(int X, int Y) {
			FiniteGrid oldRegion = tiles.GetRegion (X, Y);

			if (oldRegion == null || !oldRegion.presented) {

				if (oldRegion == null) {
					_LoadRegion (X, Y, region => {
						if(region != null) {
							_PresentRegion (X, Y, region);
						}
					});
				} else {
					_PresentRegion (X, Y, oldRegion);
				}
			}
		}
		public void HideRegion(int X, int Y) {
			FiniteGrid grid = tiles.GetRegion (X, Y);

			if(grid != null && grid.presented) {
				_HideRegion (X, Y, grid);
			}
		}

		public void LoadRegion(int X, int Y) {
			_LoadRegion (X, Y, null);
		}
		void _LoadRegion(int X, int Y, Action<FiniteGrid> callback) {
			Point p = new Point (X, Y);
			if (gridDelegate != null && !loadingTiles.Contains(p)) {

				FiniteGrid region = tiles.GetRegion (X, Y);

				if (region == null) {
					loadingTiles.Add (p);

					gridDelegate.LoadTiles (X, Y, newRegion => {
						loadingTiles.Remove(p);
						region = tiles.GetRegion(X, Y);

						if(region != null && region.presented) _HideRegion(X, Y, region);

						if (newRegion != null) {
							newRegion.presented = false;

							newRegion.LoadExtra ();

							tiles.SetRegion (X, Y, newRegion);
						}

						if(callback != null) callback(newRegion);
					});
				}
			}
		}
		public void SaveAllRegion() {
			foreach (FiniteGrid region in GetRegions()) {
				SaveRegion (region.regionX, region.regionY);
			}
		}
		public void SaveRegion(int X, int Y, bool unload = false) {
			if (gridDelegate != null) {
				FiniteGrid region = tiles.GetRegion (X, Y);

				if (region != null) {
					region.SaveExtra ();

					gridDelegate.SaveTiles (X, Y, region);
				}
			}

			if (unload) {
				UnloadRegion (X, Y);
			}
		}
		public void UnloadContainingRegion(int x, int y) {
			UnloadRegion(Mathf.FloorToInt(x / (float) Grid.REGION_SIZE), Mathf.FloorToInt(y / (float) Grid.REGION_SIZE));
		}
		public void UnloadRegion(int X, int Y) {
			FiniteGrid grid = GetRegion (X, Y);

			if(grid != null) {
				if (grid.presented) {
					_HideRegion (X, Y, grid);
				}

				tiles.ClearRegion (X, Y);
			}
		}

		void _PresentRegion(int X, int Y, FiniteGrid grid) {
			foreach (GridListener listener in gridListeners) {
				listener.OnShowRegion (X, Y);
			}

			int bx = X * REGION_SIZE, by = Y * REGION_SIZE;

			for (int i = 0; i < REGION_SIZE; i++) {
				for (int j = 0; j < REGION_SIZE; j++) {
					Tile tile = grid.Get (i, j);

					if (tile != null && tile.tileGOCenter) {
						Component component = tileGOs.TryCreateTileGO (atlas [tile.id], bx + i, by + j, (xx, yy) => { });

						if (component == null) {
							tile.tileGOCenter = false;
							tile.id = 0;
						} else if (component is TileBehaviour) {
							if(Application.isPlaying) RegisterTileGO (tile, component as TileBehaviour);
						}
					}
				}
			}

			grid.presented = true;
		}
		void _HideRegion(int X, int Y, FiniteGrid grid) {
			foreach (GridListener listener in gridListeners) {
				listener.OnHideRegion (X, Y);
			}

			int bx = X * REGION_SIZE, by = Y * REGION_SIZE;

			for (int i = 0; i < REGION_SIZE; i++) {
				for (int j = 0; j < REGION_SIZE; j++) {
					Tile tile = grid.Get (i, j);

					if (tile != null && tile.tileGOCenter) {
						Component component = tileGOs.DestroyTileGO (bx + i, by + j, (xx, yy) => { });

						if (component is TileBehaviour && Application.isPlaying) {
							UnregisterTileGO (tile, component as TileBehaviour);
						}
					}
				}
			}

			grid.presented = false;
		}

		#endregion

		#region REGION GET/SET

		public FiniteGrid GetRegion(int X, int Y) {
			return tiles.GetRegion (X, Y);
		}

		public FiniteGrid GetContainingRegion(int x, int y) {
			return tiles.GetContainingRegion (x, y);
		}

		public IEnumerable<FiniteGrid> GetRegions() {
			return tiles.GetRegions ();
		}

		public void SetRegion(int X, int Y, FiniteGrid region, bool present = true) {
			FiniteGrid oldRegion = tiles.GetRegion(X, Y);

			if (oldRegion != null && oldRegion.presented) {
				_HideRegion (X, Y, oldRegion);
			}

			tiles.SetRegion (X, Y, region);
			region.__X = X;
			region.__Y = Y;

			if (present) {
				_PresentRegion (X, Y, region);
			}
		}

		#endregion

		#region TILE GET/SET

		public Tile Get(int x, int y) {
			Tile tile =  tiles.Get (x, y) as Tile;

			return tile;
		}
		public Tile Get(int x, int y, out Component tileComponent) {
			Tile tile = tiles.Get(x, y) as Tile;

			tileComponent = tileGOs.GetComponent(x, y);

			if(tileComponent != null && tileComponent is TileBehaviour) {
				return (tileComponent as TileBehaviour).tile;
			} else {
				return tile;
			}
		}
		public Tile Get(int x, int y, out TileBehaviour tileBehaviour) {
			return Get (x, y, out tileBehaviour);
		}
		public TileShape GetShape(int x, int y) {
			Tile tile = tiles.Get (x, y) as Tile;

			return tile == null ? TileShape.EMPTY : atlas[tile.id].shape;
		}
		public int GetId(int x, int y) {
			Tile tile = tiles.Get (x, y) as Tile;

			return tile == null ? 0 : tile.id;
		}
		public int GetId(int x, int y, out Component tileComponent) {
			Tile tile = tiles.Get(x, y) as Tile;

			tileComponent = tileGOs.GetComponent(x, y);

			if(tileComponent != null && tileComponent is TileBehaviour) {
				return (tileComponent as TileBehaviour).tile.id;
			} else {
				return tile != null ? tile.id : 0;
			}
		}
		public int GetId(int x, int y, out TileBehaviour tileBehaviour) {
			return GetId (x, y, out tileBehaviour);
		}
		public int GetSubId(int x, int y) {
			Tile tile = tiles.Get (x, y) as Tile;

			return tile == null ? 0 : tile.subId;
		}
		public Component GetTileComponent(int x, int y) {
			return tileGOs.GetComponent (x, y);
		}
		public TileBehaviour GetTileBehaviour(int x, int y) {
			return tileGOs.GetTileBehaviour (x, y);
		}

		public bool CanSet(int x, int y, int id) {
			TileInfo info = atlas [id];
			Tile tile = tiles.Get (x, y);

			if (tile == null) return true;

			if (info.tileGO == null) {
				return tile.tileGOCenter || tileGOs.GetTileGO (x, y) == null;
			} else {
				TileBehaviour behaviour = info.tileGO.GetComponent<TileBehaviour> ();

				if (behaviour == null) {
					return tile.tileGOCenter || tileGOs.GetTileGO (x, y) == null;
				} else {
					return !tileGOs.HasOverlap (x, y, behaviour);
				}
			}
		}
			
		public void Set(int x, int y, int id, int subid, float state1, float state2, float state3) {
			FiniteGrid region;
			Tile tile = GetOrCreate (x, y, out region);

			if (showOnSet && !region.presented) _PresentRegion (region.regionX, region.regionY, region);

			_Set (tile, x, y, id, subid, state1, state2, state3, region.presented, true);
		}
		public void SetId(int x, int y, int id, int subId = 0) {
			FiniteGrid region;
			Tile tile = GetOrCreate (x, y, out region);

			if (showOnSet && !region.presented) _PresentRegion (region.regionX, region.regionY, region);

			_Set (tile, x, y, id, subId, tile.state1, tile.state2, tile.state3, region.presented, true);
		}
		public void SetSubId(int x, int y, int subId) {
			FiniteGrid region;
			Tile tile = GetOrCreate (x, y, out region);
			int oldSubId = tile.subId;

			tile.subId = subId;

			if (showOnSet && !region.presented) _PresentRegion (region.regionX, region.regionY, region);

			if (region.presented) {
				foreach (GridListener listener in gridListeners) {
					listener.OnSetSubId (x, y, tile, oldSubId);
				}
			}
		}
		public void SetState(int x, int y, float state1, float state2 = float.NegativeInfinity, float state3 = float.NegativeInfinity) {
			FiniteGrid region;
			Tile tile = GetOrCreate (x, y, out region);

			if (showOnSet && !region.presented) _PresentRegion (region.regionX, region.regionY, region);

			float oldState1 = tile.state1, oldState2 = tile.state2, oldState3 = tile.state3;

			tile.state1 = state1;
			if (state2 != float.NegativeInfinity) tile.state2 = state2;
			if (state3 != float.NegativeInfinity) tile.state3 = state3;

			if (region.presented) {
				foreach (GridListener listener in gridListeners) {
					listener.OnSetState (x, y, tile, oldState1, oldState2, oldState3);
				}
			}
		}
		public void Clear(int x, int y) {
			Tile tile = tiles.Get (x, y);

			if (tile != null) {
				_Clear (tile, x, y, true);
			}
		}

		public delegate Tile SetCallback (int x, int y);
		public delegate int SetIntCallback (int x, int y);
		public delegate void AreaAction(int xInArea, int yInArea, int xInRegion, int yInRegion, int xInGrid, int yInGrid, FiniteGrid region);
		public void Set(int x, int y, int width, int height, SetIntCallback callback, bool ignoreEmpty = false) {
			ExecuteAreaAction (x, y, width, height, (xa, ya, xr, yr, xg, yg, region) => {
				int newId = callback(xa, ya);

				if(newId == 0) {
					if(!ignoreEmpty) {
						Tile tile = region.Get(xr, yr);

						if (tile != null) _Clear(tile, xg, yg, false);
					}
				} else {
					Tile tile = region.GetOrCreate(xr, yr);

					_Set(tile, xg, yg, newId, 0, 0, 0, 0, region.presented, false);
				}
			});

			foreach(GridListener listener in gridListeners) {
				listener.OnSet (x, y, width, height);
			}
		}
		public void Set(int x, int y, int width, int height, SetCallback callback, bool ignoreEmpty = false) {
			ExecuteAreaAction (x, y, width, height, (xa, ya, xr, yr, xg, yg, region) => {
				Tile newTile = callback(xa, ya);

				if(newTile == null) {
					if(!ignoreEmpty) {
						Tile tile = region.Get(xr, yr);

						if(tile != null) _Clear(tile, xg, yg, false);
					}
				} else {
					Tile tile = region.GetOrCreate(xr, yr);

					_Set(tile, xg, yg, newTile.id, newTile.subId, newTile.state1, newTile.state2, newTile.state3, region.presented, false);
				}
			});

			foreach(GridListener listener in gridListeners) {
				listener.OnSet (x, y, width, height);
			}
		}

		public void Set(int x, int y, int[,] ids, bool ignoreEmpty = false) {
			ExecuteAreaAction (x, y, ids.GetLength (0), ids.GetLength (1), (xa, ya, xr, yr, xg, yg, region) => {
				int newId = ids[xa, ya];

				if(newId == 0) {
					if(!ignoreEmpty) {
						Tile tile = region.Get(xr, yr);

						if (tile != null) _Clear(tile, xg, yg, false);
					}
				} else {
					Tile tile = region.GetOrCreate(xr, yr);

					_Set(tile, xg, yg, newId, 0, 0, 0, 0, region.presented, false);
				}
			});

			foreach(GridListener listener in gridListeners) {
				listener.OnSet (x, y, ids.GetLength(0), ids.GetLength(1));
			}
		}
		public void Set(int x, int y, Tile[,] t, bool ignoreEmpty = false) {
			ExecuteAreaAction (x, y, t.GetLength (0), t.GetLength (1), (xa, ya, xr, yr, xg, yg, region) => {
				Tile newTile = t[xa, ya];

				if(newTile == null) {
					if(!ignoreEmpty) {
						Tile tile = region.Get(xr, yr);

						if(tile != null) _Clear(tile, xg, yg, false);
					}
				} else {
					Tile tile = region.GetOrCreate(xr, yr);

					_Set(tile, xg, yg, newTile.id, newTile.subId, newTile.state1, newTile.state2, newTile.state3, region.presented, false);
				}
			});

			foreach(GridListener listener in gridListeners) {
				listener.OnSet (x, y, t.GetLength(0), t.GetLength(1));
			}
		}
		public void Clear(int x, int y, int width, int height) {
			ExecuteAreaAction (x, y, width, height, (xa, ya, xr, yr, xg, yg, region) => {
				Tile tile = region.Get (xr, yr);

				if (tile != null) _Clear (tile, xg, yg, false);
			});

			foreach (GridListener listener in gridListeners) {
				listener.OnSet (x, y, width, height);
			}
		}

		void ExecuteAreaAction(int x, int y, int width, int height, AreaAction action) {
			int minRegionX = Mathf.FloorToInt (x / (float)Grid.REGION_SIZE);
			int minRegionY = Mathf.FloorToInt (y / (float)Grid.REGION_SIZE);
			int maxRegionX = Mathf.FloorToInt ((x + width) / (float)Grid.REGION_SIZE);
			int maxRegionY = Mathf.FloorToInt ((y + height) / (float)Grid.REGION_SIZE);

			for (int regionX = minRegionX; regionX <= maxRegionX; regionX++) {
				for (int regionY = minRegionY; regionY <= maxRegionY; regionY++) {
					FiniteGrid region = tiles.GetOrCreateRegion (regionX, regionY);

					if (showOnSet && !region.presented) _PresentRegion (regionX, regionY, region);

					int gridStartX = regionX * Grid.REGION_SIZE;
					int gridStartY = regionY * Grid.REGION_SIZE;

					int minX = Mathf.Max (x, gridStartX);
					int minY = Mathf.Max (y, gridStartY);
					int maxX = Mathf.Min (x + width, (regionX + 1) * Grid.REGION_SIZE);
					int maxY = Mathf.Min (y + height, (regionY + 1) * Grid.REGION_SIZE);

					for (int i = minX; i < maxX; i++) {
						for (int j = minY; j < maxY; j++) {
							action (i - x, j - y, i - gridStartX, j - gridStartY, i, j, region);
						}
					}
				}
			}
		}

		void _Clear(Tile tile, int x, int y, bool _signal) {
			Component component = tileGOs.GetComponent (x, y);

			tile.dictionary = null;

			if (component == null) {
				tile.id = 0;
				tile.subId = 0;
				tile.tileGOCenter = false;

				// POTENTIAL BUG : make sure it can be called even if we don't check it's presented
				if (_signal) {
					foreach (GridListener listener in gridListeners) {
						listener.OnSet (x, y, tile);
					}
				}
			} else {
				if (component is TileBehaviour) {
					x = (component as TileBehaviour).x;
					y = (component as TileBehaviour).y; 
				}

				tileGOs.DestroyTileGO (x, y, (xx, yy) => {
					Tile other = tiles.Get(xx, yy);

					if(other != null) {
						other.id = 0;
						other.subId = 0;

						// POTENTIAL BUG : make sure it can be called even if we don't check it's presented
						foreach (GridListener listener in gridListeners) {
							listener.OnSet (xx, yy, other);
						}
					}
				});

				if(Application.isPlaying) UnregisterTileGO ((component as TileBehaviour).tile, component as TileBehaviour);

				if (tile.tileGOCenter) {
					tile.tileGOCenter = false;
				} else {
					Get (x, y).tileGOCenter = false;
				}
			}
		}

		void _Set(Tile tile, int x, int y, int id, int subid, float state1, float state2, float state3, bool show, bool signal) {
			TileInfo info = atlas.GetTile (id);

			if (tile.tileGOCenter) {
				_Clear (tile, x, y, true);
			} else if (tileGOs.GetTileGO (x, y) != null) {
				Debug.LogError ("[Gridlike] Impossible to place a tile here: a tile GO occupies this tile");
				return;
			}

			if (show) {
				if (info.tileGO != null) {
					Component component = tileGOs.TryCreateTileGO (info, x, y, (xx, yy) => {
						Tile otherTile = tiles.Get (xx, yy);

						if (otherTile != null) {
							otherTile.id = 0;
							otherTile.subId = 0;

							foreach (GridListener listener in gridListeners) {
								listener.OnSet (xx, yy, otherTile);
							}
						}
					});
						
					if(component == null) {
						Debug.LogError ("[Gridlike] Impossible to place a tile GO here: a tile GO already occupies these tiles");
						return;
					} else if(component is TileBehaviour && Application.isPlaying) {
						(component as TileBehaviour)._grid = this;

						RegisterTileGO (tile, component as TileBehaviour);
					}

					tile.id = id;
					tile.subId = subid;

					tile.tileGOCenter = true;
				} else {
					tile.id = id;
					tile.subId = subid;

					if (signal) {
						foreach (GridListener listener in gridListeners) {
							listener.OnSet (x, y, tile);
						}
					}
				}
			}

			tile.id = id;
			tile.tileGOCenter = info.tileGO != null;
			tile.subId = subid;

			tile.state1 = state1;
			tile.state2 = state2;
			tile.state3 = state3;
		}

		Tile GetOrCreate(int x, int y, out FiniteGrid region) {
			Tile tile = tiles.Get (x, y, out region) as Tile;

			if (tile == null) {
				tile = new Tile ();

				region = tiles.Set (x, y, tile);
			}

			return tile;
		}

		#endregion

		public void SaveExtra() {
			foreach (FiniteGrid region in tiles.GetRegions()) {
				region.SaveExtra ();
			}
		}
		public void LoadExtra() {
			foreach (FiniteGrid region in tiles.GetRegions()) {
				region.LoadExtra ();
			}
		}

		#region TILE GO DATA

		void RegisterTileGO(Tile tile, TileBehaviour behaviour) {
			behaviour._grid = this;

			behaviour.OnShow ();

			if (!string.IsNullOrEmpty (tile.name)) {
				List<TileBehaviour> list;

				if (!nameToTileGO.TryGetValue (tile.name, out list)) {
					list = new List<TileBehaviour> ();
				}

				list.Add (behaviour);
			}
		}
		void UnregisterTileGO(Tile tile, TileBehaviour behaviour) {
			if (!string.IsNullOrEmpty (tile.name)) {
				List<TileBehaviour> list = nameToTileGO [tile.name];

				list.Remove (behaviour);
			}

			behaviour.OnHide ();
		}

		public List<TileBehaviour> GetTileBehaviours(string name) {
			List<TileBehaviour> list = null;

			nameToTileGO.TryGetValue (name, out list);

			return list;
		}
		public TileBehaviour GetTileBehaviour(string name) {
			List<TileBehaviour> list = null;

			nameToTileGO.TryGetValue (name, out list);

			if (list != null && list.Count > 0) {
				return list [0];
			} else {
				return null;
			}
		}

		#endregion

		#region REFERENTIAL

		public Vector2 TileCenterInWorld(int x, int y) {
			return transform.TransformPoint (TileCenterInTransform (x, y));
		}
		public Vector2 TileCenterInTransform(int x, int y) {
			return new Vector2 (x + 0.5f, y + 0.5f); 
		}
		public Vector2 TileSpaceToTransform(float x, float y) {
			return new Vector2 (x, y);
		}

		public void WorldToGrid(Vector2 position, out int x, out int y) {
			position = transform.InverseTransformPoint (position);

			TransformToGrid (position, out x, out y);
		}

		public void TransformToGrid(Vector2 position, out int x, out int y) {
			x = Mathf.FloorToInt((float)position.x);
			y = Mathf.FloorToInt((float)position.y);
		}

		#endregion

		#region GRID FACTORIES

		public static Grid CreateGrid(Vector2 position, TileAtlas atlas, bool useRenderer = true, bool useCollider = true) {
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			return grid;
		}
		public static Grid CreateGrid(Vector2 position, TileAtlas atlas, Tile[,] tiles, bool useRenderer = true, bool useCollider = true) {		
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			grid._SetTiles (tiles);

			return grid;
		}

		public static Grid CreateSaveGrid(Vector2 position, TileAtlas atlas, string path, bool usePersistentPath = true, bool useRenderer = true, bool useCollider = true) {
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			GridSaver saver = grid.gameObject.AddComponent<GridSaver> ();
			saver.path = path;
			saver.usePersistentPath = usePersistentPath;

			return grid;
		}
		public static Grid CreateSaveGrid(Vector2 position, TileAtlas atlas, Tile[,] tiles, string path, bool usePersistentPath = true, bool useRenderer = true, bool useCollider = true) {
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			GridSaver saver = grid.gameObject.AddComponent<GridSaver> ();
			saver.path = path;
			saver.usePersistentPath = usePersistentPath;

			grid._SetTiles (tiles);

			return grid;
		}

		public static Grid CreateProceduralGrid<A>(Vector2 position, TileAtlas atlas, string path, bool usePersistentPath = true, bool useRenderer = true, bool useCollider = true) where A : GridGeneratorAlgorithm {
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			GridGenerator generator = grid.gameObject.AddComponent<GridGenerator> ();
			generator.path = path;
			generator.usePersistentPath = usePersistentPath;

			grid.gameObject.AddComponent<A> ();

			return grid;
		}
		public static Grid CreateProceduralGrid<A>(Vector2 position, TileAtlas atlas, Tile[,] tiles, string path, bool usePersistentPath = true, bool useRenderer = true, bool useCollider = true) where A : GridGeneratorAlgorithm {
			Grid grid = _CreateGrid (position, atlas, useRenderer, useCollider);

			GridGenerator generator = grid.gameObject.AddComponent<GridGenerator> ();
			generator.path = path;
			generator.usePersistentPath = usePersistentPath;

			grid.gameObject.AddComponent<A> ();

			grid._SetTiles (tiles);

			return grid;
		}

		static Grid _CreateGrid(Vector2 position, TileAtlas atlas, bool useRenderer, bool useCollider) {
			GameObject obj = new GameObject ("grid");
			obj.transform.position = position;

			Grid grid = obj.AddComponent<Grid> ();
			grid.atlas = atlas;

			if(useRenderer) obj.AddComponent<GridRenderer> ();
			if(useCollider) obj.AddComponent<GridCollider> ();

			return grid;
		}

		void _SetTiles(Tile[,] tiles) {
			int w = Mathf.CeilToInt(tiles.GetLength (0) / ((float)REGION_SIZE));
			int h = Mathf.CeilToInt(tiles.GetLength (1) / ((float)REGION_SIZE));

			for (int i = 0; i < w; i++) {
				for (int j = 0; j < h; j++) {
					FiniteGrid region = new FiniteGrid(i, j, REGION_SIZE, tiles, i * REGION_SIZE, j * REGION_SIZE);

					SetRegion (i, j, region);
				}
			}
		}

		#endregion

		#region GRID QUERY

		public static List<Grid> GetAllGrids() {
			return grids;
		}
		public static Grid GetFirstGrid() {
			if (grids != null && grids.Count > 0) {
				return grids [0];
			} else {
				return null;
			}
		}

		#endregion
	}
}