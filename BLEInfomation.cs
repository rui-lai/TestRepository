using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BLE
{
    public class BLEInfomation
    {
        private string Id;
        private string Name;
        private bool isConnectable;
        private string Service;
        private string Characteristic;

        public BLEInfomation()
        {

        }

        public string ID
        {
            get { return Id; }
            set { Id = value; }
        }
        public string NAME
        {
            get { return Name; }
            set { Name = value; }
        }
        public bool IsConnectable
        {
            get { return isConnectable; }
            set { isConnectable = value; }
        }
        public string SERVICE
        {
            get { return Service; }
            set { Service = value; }
        }
        public string CHAR
        {
            get { return Characteristic; }
            set { Characteristic = value; }
        }
    }
}

