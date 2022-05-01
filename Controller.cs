using System.Collections;
using UnityEngine;

namespace BLE
{
    //public enum Status { Horizontial, Vertical, Slash, Backslash, Circle, Static }
    public enum Status { NoTouch, Touch };
    public enum Gestures {Right, Left, Forward, Back, None};
    //管理手势操控
    public class Controller :MonoBehaviour
    {
        public GameObject ScanBLE;
        private Status stat;
        private Gestures gs;
        private int direction = -1;

        private void Start()
        {
            
        }

        private void Update()
        {
            stat = ScanBLE.GetComponent<Test>().stat;
            ControlManagement(stat);
            switch (direction)
            {
                case 0:
                    OnRight();
                    break;
                case 1:
                    OnLeft();
                    break;
                case 2:
                    OnForward();
                    break;
                case 3:
                    OnBack();
                    break;
                default:
                    break;
            }
        }
        public void ControlManagement(Status status)
        {
            //Debug.LogWarning(gs.ToString());
            switch (status)
            {
                case Status.NoTouch:
                    OnNoTouch();
                    break;
                case Status.Touch:
                    OnTouch();
                    break;
            }
        }
        private void OnRight()
        {
            Debug.Log("Right");
            this.transform.Rotate(0, 0.1F, 0, Space.World);
        }
        private void OnLeft()
        {
            Debug.Log("Left");
            this.transform.Rotate(0, -0.1F, 0, Space.World);
        }
        private void OnForward()
        {
            Debug.Log("Forward");
            this.transform.Rotate(0.1F, 0, 0, Space.World);
        }
        private void OnBack()
        {
            Debug.Log("Back");
            this.transform.Rotate(-0.1F, 0, 0, Space.World);
        }
        private void OnNoTouch()
        {
            direction = -1;
            //Debug.Log("NoTouch");
            //this.transform.Rotate(0, 0, 0, Space.Self);
        }
        private void OnTouch()
        {
            gs = ScanBLE.GetComponent<Test>().gesture;
            //Debug.Log("Touch");
            //this.transform.Rotate(0, 0.1F, 0, Space.Self);
            switch (gs)
            {
                case Gestures.Right:
                    direction = 0;
                    //OnRight();
                    break;
                case Gestures.Left:
                    direction = 1;
                    //OnLeft();
                    break;
                case Gestures.Forward:
                    direction = 2;
                    //OnForward();
                    break;
                case Gestures.Back:
                    direction = 3;
                    //OnBack();
                    break;
                case Gestures.None:
                    break;
            }
        }


    }
}