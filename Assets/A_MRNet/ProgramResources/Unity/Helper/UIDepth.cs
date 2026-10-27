using UnityEngine;
using System.Collections;
namespace Unity.Helper {
    public class UIDepth : MonoBehaviour {

        public int order;
        public bool isUI = true;
        void Start() {
            if (isUI) {
                Canvas canvas = GetComponent<Canvas>();
                if (canvas == null) {
                    canvas = gameObject.AddComponent<Canvas>();
                }
                canvas.overrideSorting = true;
                canvas.sortingOrder = order;
            } else {
                Renderer[] renders = GetComponentsInChildren<MeshRenderer>();
                foreach (Renderer render in renders) {
                    render.sortingOrder = order;
                }
                renders = GetComponentsInChildren<SpriteRenderer>();
                foreach (Renderer render in renders) {
                    render.sortingOrder = order;
                }
            }
        }

        public void ResetOrder() {
            if (isUI) {
                Canvas canvas = GetComponent<Canvas>();
                if (canvas == null) {
                    canvas = gameObject.AddComponent<Canvas>();
                }
                canvas.overrideSorting = true;
                canvas.sortingOrder = order;
            } else {
                Renderer[] renders = GetComponentsInChildren<MeshRenderer>();
                foreach (Renderer render in renders) {
                    render.sortingOrder = order;
                }
                renders = GetComponentsInChildren<SpriteRenderer>();
                foreach (Renderer render in renders) {
                    render.sortingOrder = order;
                }

                //			Renderer render = GetComponent<MeshRenderer>();
                //			render.sortingOrder = order;
            }
        }
    }
}