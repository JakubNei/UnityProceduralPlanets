using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class InputHelper : MonoBehaviour
{
	[TextArea]
	public string text =
@"G - toggle walk on planet mode
L - toggle light
C - look towards planet

W,S,A,D - move front, back, left, right
space, left control - move up, down

left shift - move faster
mouse wheel - change move speed

Q, E - rotate roll
mouse - rotate yaw, pitch

R - regenerate planet
F3 - toggle profiler

F5 - save position 1
F6 - load position 1
F7 - save position 2
F8 - load position 2

escape - exit";

	public float keyNotPressedForSeconds = 0;

	private void Update()
	{
		if (Input.anyKey) keyNotPressedForSeconds = 0;
		else keyNotPressedForSeconds += Time.deltaTime;
	}

	private void OnGUI()
	{
		var restore = GUI.skin.label.alignment;
		GUI.skin.label.alignment = TextAnchor.UpperRight;
		if (keyNotPressedForSeconds > 1)
			GUI.Label(new Rect(5, 5, Screen.width - 10, Screen.height - 10), text);

		GUI.skin.label.alignment = restore;
	}

}

