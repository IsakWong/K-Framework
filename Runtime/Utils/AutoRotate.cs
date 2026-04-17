using UnityEngine;

namespace K1
{
    [ExecuteInEditMode]
    public class AutoRotate : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            var x = 500 * Time.deltaTime;
            var y = 700 * Time.deltaTime;
            transform.Rotate(Vector3.up, x, Space.World);
            //transform.Rotate(Vector3.right, -y, Space.World);
        }
    }
}