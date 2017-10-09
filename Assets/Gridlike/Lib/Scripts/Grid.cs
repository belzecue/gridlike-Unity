﻿using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Grid : MonoBehaviour {

	public const int REGION_SIZE = 50;

	[SerializeField] List<GridListener> gridListeners;

	// TODO optimize
	[SerializeField] InfiniteGrid tiles;

	[SerializeField] float _tileSize = 1;

	[HideInInspector] public TileAtlas atlas;

	public float tileSize {
		get { return _tileSize; }
		set { if(value != _tileSize) SetTileSize (value); }
	}

	void Init() {
		if (tiles == null) {
			gridListeners = new List<GridListener> ();

			tiles = new InfiniteGrid (REGION_SIZE);

			foreach (GridListener listener in GetComponents<GridListener> ()) {
				listener.ResetGrid ();
			}
		}
	}

	#region UNITY EVENTS

	void Reset() {
		tiles = null;

		Init ();
	}
	void Awake() {
		Init ();
	}

	void OnDestroy() {
		foreach (GridListener listener in GetComponents<GridListener> ()) {
			listener.ResetGrid ();
		}
	}

	#endregion 

	#region GRID LISTENERS

	public void AddListener(GridListener listener) {
		gridListeners.Add (listener);

		Debug.Log ("[Add] Listener count=" + gridListeners.Count);
	}
	public void RemoveListener(GridListener listener) {
		gridListeners.Remove (listener);

		Debug.Log ("[Remove] Listener count=" + gridListeners.Count);
	}

	#endregion

	#region REGION PRESENTING

	public void PresentAllAgain() {
		List<FiniteGrid> regions = tiles.GetRegions ();
		foreach (FiniteGrid regionPosition in regions) {
			HideRegion (regionPosition.x, regionPosition.y);
		}

		foreach (FiniteGrid regionPosition in regions) {
			PresentRegion (regionPosition.x, regionPosition.y);
		}
	}

	public void PresentAll() {
		foreach (FiniteGrid region in tiles.GetRegions()) {
			PresentRegion (region.x, region.y);
		}
	}
	public void HideAll() {
		foreach (FiniteGrid region in tiles.GetRegions()) {
			HideRegion (region.x, region.y);
		}
	}

	public void PresentRegion(int X, int Y) {
		FiniteGrid grid = tiles.GetRegion (X, Y);

		if(!grid.presented) {
			foreach (GridListener listener in gridListeners) {
				listener.OnShowRegion (X, Y);
			}

			grid.presented = true;
		}
	}
	public void HideRegion(int X, int Y) {
		FiniteGrid grid = tiles.GetRegion (X, Y);

		if(grid.presented) {
			foreach (GridListener listener in gridListeners) {
				listener.OnHideRegion (X, Y);
			}

			grid.presented = false;
		}
	}

	#endregion

	#region REGION LOADING/UNLOADING

	public void LoadRegion(int X, int Y) {
		// TODO
	}
	public void UnloadRegion(int X, int Y) {
		// TODO
	}

	#endregion

	#region REGION SET/GET

	public FiniteGrid GetRegion(int X, int Y) {
		return tiles.GetRegion (X, Y);
	}

	public FiniteGrid GetContainingRegion(int x, int y) {
		return tiles.GetContainingRegion (x, y);
	}

	public IEnumerable<FiniteGrid> GetRegions() {
		return tiles.GetRegions ();
	}

	// requirement: width and height = REGION_SIZE
	public void SetRegion(int X, int Y, TileInfo[,] tiles) {

	}

	#endregion

	#region TILE SET/GET

	public Tile Get(int x, int y) {
		return tiles.Get (x, y) as Tile;
	}
	public TileShape GetShape(int x, int y) {
		Tile tile = tiles.Get (x, y) as Tile;

		return tile == null ? TileShape.EMPTY : tile.shape;
	}
	public int GetId(int x, int y) {
		Tile tile = tiles.Get (x, y) as Tile;

		return tile == null ? 0 : tile.id;
	}
	public int GetSubId(int x, int y) {
		Tile tile = tiles.Get (x, y) as Tile;

		return tile == null ? 0 : tile.subId;
	}

	public void Set(int x, int y, int id, int subid, int state1, int state2, int state3) {
		Tile tile = GetOrCreate (x, y);

		tile.shape = atlas.GetTile(id).shape;

		tile.id = id;
		tile.subId = subid;

		tile.state1 = state1;
		tile.state2 = state2;
		tile.state3 = state3;

		foreach (GridListener listener in gridListeners) {
			listener.OnSet (x, y, tile);
		}
	}
	public void SetId(int x, int y, int id, int subId = int.MinValue) {
		Tile tile = GetOrCreate (x, y);
		int oldId = tile.id, oldSubId = tile.subId;

		tile.id = id;
		tile.shape = atlas.GetTile(id).shape;
		if (subId != int.MinValue) tile.subId = subId;

		foreach (GridListener listener in gridListeners) {
			listener.OnSetId (x, y, tile, oldId, oldSubId);
		}
	}
	public void SetSubId(int x, int y, int subId) {
		Tile tile = GetOrCreate (x, y);
		int oldId = tile.id, oldSubId = tile.subId;

		tile.subId = subId;

		foreach (GridListener listener in gridListeners) {
			listener.OnSetId (x, y, tile, oldId, oldSubId);
		}
	}
	public void SetState(int x, int y, float state1, float state2 = float.NegativeInfinity, float state3 = float.NegativeInfinity) {
		Tile tile = GetOrCreate (x, y);

		float oldState1 = tile.state1, oldState2 = tile.state2, oldState3 = tile.state3;

		tile.state1 = state1;
		if (state2 != float.NegativeInfinity) tile.state2 = state2;
		if (state3 != float.NegativeInfinity) tile.state3 = state3;

		foreach (GridListener listener in gridListeners) {
			listener.OnSetState (x, y, tile, oldState1, oldState2, oldState3);
		}
	}
		
	Tile GetOrCreate(int x, int y) {
		Tile tile = tiles.Get (x, y) as Tile;

		if (tile == null) {
			tile = new Tile ();

			tiles.Set (x, y, tile);
		}

		return tile;
	}

	#endregion

	#region TILE SIZE

	public void SetTileSize(float tileSize) {
		this._tileSize = tileSize;

		foreach (GridListener listeners in gridListeners) {
			listeners.OnTileSizeChange ();
		}
	}

	#endregion

	#region REFERENTIAL

	public Vector2 TileCenterInWorld(int x, int y) {
		return transform.TransformPoint (TileCenterInTransform (x, y));
	}
	public Vector2 TileCenterInTransform(int x, int y) {
		return new Vector2 ((x + 0.5f) * _tileSize, (y + 0.5f) * _tileSize); 
	}
	public Vector2 TileSpaceToTransform(float x, float y) {
		return new Vector2 (x * _tileSize, y * _tileSize);
	}

	public void WorldToGrid(Vector2 position, out int x, out int y) {
		position = transform.InverseTransformPoint (position);

		TransformToGrid (position, out x, out y);
	}

	public void TransformToGrid(Vector2 position, out int x, out int y) {
		x = Mathf.FloorToInt(((float)position.x) / tileSize);
		y = Mathf.FloorToInt(((float)position.y) / tileSize);
	}

	#endregion
}