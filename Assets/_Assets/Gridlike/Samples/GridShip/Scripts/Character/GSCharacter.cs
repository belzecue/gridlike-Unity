﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ICubeStorage {

	event Action<int> onAddCube;
	event Action<int> onRemoveCube;

	int GetCubeCount ();
	bool ConsumeCubes (int count);
	void AddCubes (int count);
}

public class GSCharacter : MonoBehaviour, ICubeStorage {

	public event Action<int> onAddCube;
	public event Action<int> onRemoveCube;

	public int cubeCount;

	public Bow bow;
	public Pickaxe pickaxe;
	public Placer placer;

	public Tool currentTool;

	void Start () {
		GSSingleton.instance.RegisterCharacter (this);

		if (bow != null) bow._Inject (this);
		if (pickaxe != null) pickaxe._Inject (this);
		if (placer != null) placer._Inject (this);
	}

	void Update() {
		if (currentTool != null) {
			if (Input.GetMouseButtonDown (0) && !IsPointerOverUIObject ()) {
				currentTool.OnMouseDown (Camera.main.ScreenToWorldPoint (Input.mousePosition));
			}

			if (Input.GetMouseButton (0) && !IsPointerOverUIObject ()) {
				currentTool.OnMouse (Camera.main.ScreenToWorldPoint (Input.mousePosition));
			}

			if (Input.GetMouseButtonUp (0) && !IsPointerOverUIObject ()) {
				currentTool.OnMouseUp (Camera.main.ScreenToWorldPoint (Input.mousePosition));
			}

			if ((Input.GetMouseButtonDown (0) || Input.GetMouseButton (0) || Input.GetMouseButtonUp (0)) && !IsPointerOverUIObject ()) {
				currentTool.OnMouseAny (Camera.main.ScreenToWorldPoint (Input.mousePosition));
			}
		}
	}
	bool IsPointerOverUIObject() {

		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}

	#region CUBES

	public int GetCubeCount() {
		return cubeCount;
	}
	public bool ConsumeCubes(int count) {
		if (cubeCount >= count) {
			int old = cubeCount;
			cubeCount -= count;

			if (onRemoveCube != null) onRemoveCube (old);

			return true;
		} else {
			return false;
		}
	}
	public void AddCubes(int count) {
		int old = cubeCount;
		cubeCount += count;

		if (onAddCube != null) onAddCube (old);
	}

	#endregion

	#region ITEMS

	public bool HasBow() {
		return bow != null && bow.enabled;
	}
	public bool HasPickaxe() {
		return pickaxe != null && pickaxe.enabled;
	}
	public bool HasPlacer() {
		return placer != null && placer.enabled;
	}

	public void AcquireBow() {
		if (bow == null) bow = gameObject.AddComponent<Bow> ();
		bow._Inject (this);

		bow.enabled = true;
	}
	public void AcquirePickaxe() {
		if (pickaxe == null) pickaxe = gameObject.AddComponent<Pickaxe> ();
		pickaxe._Inject (this);

		pickaxe.enabled = true;
	}
	public void AcquirePlacer() {
		if (placer == null) placer = gameObject.AddComponent<Placer> ();
		placer._Inject (this);

		placer.enabled = true;
	}

	public void SelectBow() {
		currentTool = bow;
	}
	public void SelectPickaxe() {
		currentTool = pickaxe;
	}
	public void SelectPlacer() {
		currentTool = placer;
	}

	#endregion
}
