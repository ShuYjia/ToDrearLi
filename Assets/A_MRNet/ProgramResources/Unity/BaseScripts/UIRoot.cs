using UnityEngine;
using System.Collections;



namespace Unity.BaseScripts{
    
	public class UIRoot : MonoBehaviour {
		//属性项，不允许外部直接修改
		public Camera _camera;

		void Awake(){
			//_camera.aspect = 1920.0f / 1080.0f;
		}

		// Use this for initialization
		void Start () {
		
		}
		
		// Update is called once per frame
		void Update () {
		
		}
	}
}
