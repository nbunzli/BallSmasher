using UnityEngine;
using System.Collections;

// Sets an opject's position, relative to the camera position.
// Passing in (0,0) for x and y will put the object in the bottom left corner of the screen, (0.5,0.5) is the middle, (1,1) is top right, etc.
// Mostly useful in the editor, when changing the size of the play window.
public class Layout : MonoBehaviour
{
	public Vector2 RelativePosition;
	
	void Start()
	{
		float z = gameObject.transform.position.z;
		Vector3 NewPos = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth * RelativePosition.x, Camera.main.pixelHeight * RelativePosition.y, Camera.main.nearClipPlane));
		NewPos.z = z;
		gameObject.transform.position = NewPos;
	}
}
