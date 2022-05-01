using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using BLE;
using UnityEngine;
using TMPro;
using System;
using Unity.Barracuda;
using UnityEditor;

public class ScanBLE : MonoBehaviour
{
    private string targetDeviceName = "wireless wearable ring";
    private ButtonConfigHelper scanBLEConfig;
    private TextMeshPro deviceInfo;
    private TextMeshPro data;
    private Interactable connect;
    private int[] deviceData;
    private List<int> rawData;
    private List<float[]> dataBlock;
    private Model model;
    private IWorker engine;
    private Tensor output;

    private float[] accelerationx = new float[2];
    private float[] accelerationy = new float[2];
    private float[] accelerationz = new float[2];
    private float[] velocityx = new float[2];
    private float[] velocityy = new float[2];
    private float[] velocityz = new float[2];
    private float[] positionX = new float[2];
    private float[] positionY = new float[2];
    private float[] positionZ = new float[2];
    private float[] der;
    private float sstatex = 1.825f;
    private float sstatey = 0.613f;
    private float sstatez = 9.865457f;
    private float windowlength = 4.0f;
    private float displacementLimit = 500.0f;

    private int sampleNum;
    private int countx;
    private int county;
    private int countz;

    private string direction = "";

    public Status stat;
    public Gestures gesture;
    private Gestures tempGestures;
    public bool isScanningDevices = false;
    public bool isScanningServices = false;
    public bool isScanningCharacteristics = false;
    public bool isSubscribed = false;
    public bool isPrepared = false;
    private string targetService = "{6e400001-b5a3-f393-e0a9-e50e24dcca9e}";
    private string targetCharacteristics = "{6e400003-b5a3-f393-e0a9-e50e24dcca9e}";
    public string modelAssetPath = "";
    public int accelLSB;
    public double angularLSB;
    public int dataBlockSize;
    public int dataLength;
    public GameObject deviceScanButton;
    public GameObject requestConnectDialog;
    public GameObject DeviceInfomation;
    public GameObject DataDescription;
    public GameObject exampleModel;
    public BLEInfomation targetDevice;
    public GameObject connectButton;
    public NNModel modelAsset;
    private Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    // Start is called before the first frame update
    void Start()
    {
        stat = Status.NoTouch;
        gesture = Gestures.None;
        rawData = new List<int>();
        dataBlock = new List<float[]>();
        data = DataDescription.GetComponent<TextMeshPro>();
        connect = connectButton.GetComponent<Interactable>();
        deviceInfo = DeviceInfomation.GetComponent<TextMeshPro>();
        scanBLEConfig = deviceScanButton.GetComponent<ButtonConfigHelper>();
        model = ModelLoader.Load(modelAsset);
        engine = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, model);
    }

    // Update is called once per frame
    void Update()
    {
        BleApi.ScanStatus status;
        if (isScanningDevices)
        {
            //GetTargetDevice();
            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    if (!devices.ContainsKey(res.id))
                        devices[res.id] = new Dictionary<string, string>() {
                            { "name", "" },
                            { "isConnectable", "False" }
                        };
                    if (res.nameUpdated)
                        devices[res.id]["name"] = res.name;
                    if (res.isConnectableUpdated)
                        devices[res.id]["isConnectable"] = res.isConnectable.ToString();
                    // consider only devices which have a name and which are connectable
                    if (devices[res.id]["name"] == targetDeviceName && devices[res.id]["isConnectable"] == "True")
                    {
                        requestConnectDialog.SetActive(true);
                        deviceInfo.text = "Name:" + devices[res.id]["name"] + "\nId:" + res.id + "\tIsConnectable:" + devices[res.id]["isConnectable"];
                        targetDevice.ID = res.id;
                        targetDevice.NAME = res.name;
                        targetDevice.IsConnectable = res.isConnectable;
                        Debug.Log("Discovery Target Device!" + "\nName:" + devices[res.id]["name"] + "\nisConnectable:" + devices[res.id]["isConnectable"]);
                    }
                    Debug.Log("Name:" + devices[res.id]["name"] + "\nId:" + res.id + "\tIsConnectable:" + devices[res.id]["isConnectable"]);
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    Debug.Log("BLE Device Scan Complete");
                    isScanningDevices = false;
                    scanBLEConfig.MainLabelText = "ScanBLE";
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningServices)
        {
            //GetTargetService();
            BleApi.Service res = new BleApi.Service();
            do
            {
                status = BleApi.PollService(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    Debug.Log("ServiceUUID:" + res.uuid);
                    if (res.uuid.Equals(targetService))
                    {
                        isScanningServices = false;
                        Debug.Log("Discovery target Service");
                        targetDevice.SERVICE = res.uuid;
                        isScanningCharacteristics = true;
                        BleApi.ScanCharacteristics(targetDevice.ID, res.uuid);
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    connect.IsEnabled = true;
                    Debug.Log("Service Discovery Complete");
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isScanningCharacteristics)
        {
            //GetTargetCharacteristics();
            BleApi.Characteristic res = new BleApi.Characteristic();
            do
            {
                status = BleApi.PollCharacteristic(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    string name = res.userDescription != "no description available" ? res.userDescription : res.uuid;
                    Debug.Log("CharacteristicUUID:" + name);
                    if (name.Equals(targetCharacteristics))
                    {
                        //发现目标Characteristics
                        Debug.Log("Discovery target Characteristic");
                        targetDevice.CHAR = name;
                        isSubscribed = true;
                        isPrepared = true;
                        requestConnectDialog.SetActive(false);
                        BleApi.SubscribeCharacteristic(targetDevice.ID, targetDevice.SERVICE, targetDevice.CHAR, false);
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningCharacteristics = false;
                    Debug.Log("Characteristics Discovery Complete");
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                //data.text = BitConverter.ToString(res.buf, 0, res.size);
                //rawData = res.buf;
                //Debug.Log(BitConverter.ToString(res.buf, 0, res.size));
                string raw = BitConverter.ToString(res.buf, 0, res.size);
                string[] raws = raw.Split(new char[]{'-'});
                foreach(string x in raws){
                    rawData.Add(Convert.ToSByte(x, 16));
                }
                deviceData = rawData.ToArray();
                //Debug.Log(string.Join(" ", deviceData));
                rawData.Clear();
                //先对数据进行处理，然后输入模型
                if (deviceData.Length > 0)
                {
                    InputTransform();
                    if (dataBlock.Count == dataBlockSize)
                    {
                        using (var input = GetInput())
                        {
                            if (input != null)
                            {
                                //得到模型输出
                                output = engine.Execute(input).PeekOutput();
                                input.Dispose();
                                //Debug.LogWarning(string.Join(",", output.AsFloats()));
                                //对模型输出进行处理得到现在的状态
                                if (output != null)
                                {
                                    stat = GetStatus();
                                    //if (stat == Status.Touch)
                                    //{
                                    //    gesture = tempGestures;
                                    //    Debug.LogError(gesture.ToString());
                                    //}
                                    output.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }
        //显示演示模型，如果已经显示了就不用再显示了，以防update反复刷新
        if (isPrepared)
        {
            if(!exampleModel.activeInHierarchy){
                exampleModel.SetActive(true);
            }
        }
        {
            // log potential errors
            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
            BleApi.GetError(out res);
            //Debug.LogError(res.msg);
        }
    }
    //按键处理函数，触发蓝牙设备扫描
    public void StartStopDeviceScan()
    {
        if (!isScanningDevices)
        {
            //While Open a new Scan , the last Dictionary should be cleared
            devices.Clear();
            targetDevice = new BLEInfomation(); ;
            // start new scan
            Debug.Log("Start Scan BLE Device");
            //BleApi.StopDeviceScan();
            BleApi.StartDeviceScan();
            isScanningDevices = true;
            scanBLEConfig.MainLabelText = "Stop";
        }
        else
        {
            // stop scan
            isScanningDevices = false;
            targetDevice = null;
            scanBLEConfig.MainLabelText = "ScanBLE";
            BleApi.StopDeviceScan();
            BleApi.Quit();
        }
    }
    //按键处理事件，触发蓝牙设备连接，扫描目标服务、特征以及获取数据
    public void ConnectTarget()
    {
        if (targetDevice != null)
        {
            if (targetDevice.NAME != "" && targetDevice.ID != "")
            {
                if (!isScanningServices)
                {
                    BleApi.ScanServices(targetDevice.ID);
                    isScanningServices = true;
                    connect.IsEnabled = false;
                }
            }
        }
    }
    private void GetTargetDevice()
    {
        //BleApi.ScanStatus status;
        //BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
        //do
        //{
        //    status = BleApi.PollDevice(ref res, false);
        //    if (status == BleApi.ScanStatus.AVAILABLE)
        //    {
        //        if (!devices.ContainsKey(res.id))
        //            devices[res.id] = new Dictionary<string, string>() {
        //                    { "name", "" },
        //                    { "isConnectable", "False" }
        //                };
        //        if (res.nameUpdated)
        //            devices[res.id]["name"] = res.name;
        //        if (res.isConnectableUpdated)
        //            devices[res.id]["isConnectable"] = res.isConnectable.ToString();
        //        // consider only devices which have a name and which are connectable
        //        if (res.name.Equals(targetDeviceName) && res.isConnectable)
        //        {
        //            requestConnectDialog.SetActive(true);
        //            deviceInfo.text = "Name:" + devices[res.id]["name"] + "\nId:" + res.id + "\tIsConnectable:" + devices[res.id]["isConnectable"];
        //            targetDevice.ID = res.id;
        //            targetDevice.NAME = res.name;
        //            targetDevice.IsConnectable = res.isConnectable;
        //            Debug.Log("Discovery Target Device!" + "\nName:" + devices[res.id]["name"] + "\nisConnectable:" + devices[res.id]["isConnectable"]);
        //            status = BleApi.ScanStatus.FINISHED;
        //        }
        //        Debug.Log("Name:" + devices[res.id]["name"] + "\nId:" + res.id + "\tIsConnectable:" + devices[res.id]["isConnectable"]);
        //    }
        //    else if (status == BleApi.ScanStatus.FINISHED)
        //    {
        //        Debug.Log("BLE Device Scan Complete");
        //        isScanningDevices = false;
        //        scanBLEConfig.MainLabelText = "ScanBLE";
        //    }
        //} while (status == BleApi.ScanStatus.AVAILABLE);
    }
    private void GetTargetService()
    {
        //BleApi.ScanStatus status;
        //BleApi.Service res = new BleApi.Service();
        //do
        //{
        //    status = BleApi.PollService(out res, false);
        //    if (status == BleApi.ScanStatus.AVAILABLE)
        //    {
        //        Debug.Log("ServiceUUID:" + res.uuid);
        //        if (res.uuid.Equals(targetService))
        //        {
        //            isScanningServices = false;
        //            Debug.Log("Discovery target Service");
        //            targetDevice.SERVICE = res.uuid;
        //            isScanningCharacteristics = true;
        //            BleApi.ScanCharacteristics(targetDevice.ID, res.uuid);
        //        }
        //    }
        //    else if (status == BleApi.ScanStatus.FINISHED)
        //    {
        //        isScanningServices = false;
        //        connect.IsEnabled = true;
        //        Debug.Log("Service Discovery Complete");
        //    }
        //} while (status == BleApi.ScanStatus.AVAILABLE);
    }
    private void GetTargetCharacteristics()
    {
        BleApi.ScanStatus status;
        BleApi.Characteristic res = new BleApi.Characteristic();
        do
        {
            status = BleApi.PollCharacteristic(out res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                string name = res.userDescription != "no description available" ? res.userDescription : res.uuid;
                Debug.Log("CharacteristicUUID:" + name);
                if (name.Equals(targetCharacteristics))
                {
                    //发现目标Characteristics
                    Debug.Log("Discovery target Characteristic");
                    targetDevice.CHAR = name;
                    isSubscribed = true;
                    isPrepared = true;
                    requestConnectDialog.SetActive(false);
                    BleApi.SubscribeCharacteristic(targetDevice.ID, targetDevice.SERVICE, targetDevice.CHAR, false);
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                isScanningCharacteristics = false;
                Debug.Log("Characteristics Discovery Complete");
            }
        } while (status == BleApi.ScanStatus.AVAILABLE);
    }
    //private void GetData()
    //{
    //    BleApi.BLEData res = new BleApi.BLEData();
    //    while (BleApi.PollData(out res, false))
    //    {
    //        data.text = BitConverter.ToString(res.buf, 0, res.size);
    //    }
    //}
    public void CloseDialog()
    {
        requestConnectDialog.SetActive(false);
        isScanningDevices = false;
        scanBLEConfig.MainLabelText = "ScanBLE";
        BleApi.StopDeviceScan();
        BleApi.Quit();
    }
    public void CloseApp()
    {
        #if UNITY_EDITOR    //在编辑器模式下
                EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    //3.2任务写加载模型的模块，数值转换，现在是20位16进制，要转换成6位十进制
    private void InputTransform()
    {
        List<float> derData = new List<float>();
        //0-5转成加速度数据，全部截断只保留整数部分
        for (int i = 0; i < 6; i += 2)
        {
            double temp = ((deviceData[i + 1] << 8) | deviceData[i]) * 9.8 / accelLSB;
            derData.Add((float)Math.Round(temp, 3));
        }
        //6-11转成角速度数据，全部截断只保留整数部分
        for (int i = 6; i < 12; i += 2)
        {
            double temp = ((deviceData[i + 1] << 8) | deviceData[i]) * (Math.PI / 180) / angularLSB;
            derData.Add((float)Math.Round(temp, 3));
        }
        der = derData.ToArray();
        //GetGestures();
        //data.text = string.Join("," , der);
        Debug.Log(string.Join(",", der));
        //Calibrate();
        //if(sampleNum == 2048)
        //{
        //    sstatex /= 2048;
        //    sstatey /= 2048;
        //    Debug.LogError("sstatex:"+sstatex+"sstatey:"+sstatey);
        //}
        derData.Clear();
        if (dataBlock.Count < dataBlockSize)
        {
            dataBlock.Add(der);
        }
        else
        {
            //Debug.LogWarning("Full");
            dataBlock.Clear();
            dataBlock.Add(der);
        }

    }
    private Tensor GetInput()
    {
        Tensor input = null;
        if (dataBlock.Count == dataBlockSize)
        {
            float[] inputSource = copyToArray();
            input = new Tensor(1, dataBlockSize, dataLength, 1, inputSource, "input");
            //Debug.LogWarning(string.Join(",", input.DataToString()));
        }
        return input;
    }
    private Status GetStatus(){
        Status status = Status.NoTouch;
        int index = output.ArgMax()[0];
        //Debug.LogWarning(index.ToString());
        switch (index)
        {
            case 0:
                status = Status.NoTouch;
                data.text = "NoTouch";
                //Debug.Log("NoTouch");
                //Debug.Log("Horizontial");
                break;
            case 1:
                status = Status.Touch;
                data.text = "Touch";
                //Debug.Log("Touch");
                //Debug.Log("Vertical");
                break;
        }
        return status;
    }

    private void Calibrate()
    {
        sstatex += der[0];
        sstatey += der[1];
        sampleNum++;
    }

    private void GetGestures()
    {
        //Gestures g = Gestures.None;
        accelerationx[1] = der[0];
        accelerationy[1] = der[1];

        accelerationx[1] -= sstatex;
        accelerationy[1] -= sstatey;

        //给定一个窗口大小，这里窗口为2，如果加速度小于这个区间这表明没有移动
        if ((accelerationx[1] <= windowlength) && (accelerationx[1] >= -windowlength))
        { accelerationx[1] = 0; }

        if ((accelerationy[1] <= windowlength) && (accelerationy[1] >= -windowlength))
        { accelerationy[1] = 0; }

        //first X integration:
        velocityx[1] = velocityx[0] + accelerationx[0] + ((accelerationx[1] - accelerationx[0]) / 2);

        //second X integration:
        positionX[1] = positionX[0] + velocityx[0] + ((velocityx[1] - velocityx[0]) / 2);
        //positionX[1] = velocityx[0] + ((velocityx[1] - velocityx[0]) / 2);

        //first Y integration:
        velocityy[1] = velocityy[0] + accelerationy[0] + ((accelerationy[1] - accelerationy[0]) / 2);

        //second Y integration:
        positionY[1] = positionY[0] + velocityy[0] + ((velocityy[1] - velocityy[0]) / 2);
        //positionY[1] = velocityy[0] + ((velocityy[1] - velocityy[0]) / 2);

        accelerationx[0] = accelerationx[1];
        accelerationy[0] = accelerationy[1];

        velocityx[0] = velocityx[1];
        velocityy[0] = velocityy[1];

        positionX[0] = positionX[1];
        positionY[0] = positionY[1];

        //Debug.LogWarning("posX:" + positionX[1] + "posY:" + positionY[1]);

        if (direction == "")
        {
            if (positionY[1] < -displacementLimit && positionX[1] == 0)
            {
                direction = "右";
                gesture = Gestures.Right;
                Debug.LogWarning("右");
            }
            else if (positionY[1] > displacementLimit && positionX[1] == 0)
            {
                direction = "左";
                gesture = Gestures.Left;
                Debug.LogWarning("左");
            }
            else if (positionX[1] < -displacementLimit && positionY[1] == 0)
            {
                direction = "前";
                gesture = Gestures.Forward;
                Debug.LogWarning("前");
            }
            else if (positionX[1] > displacementLimit && positionY[1] == 0)
            {
                direction = "后";
                gesture = Gestures.Back;
                Debug.LogWarning("后");
            }
            //else if (positionX[1] < -displacementLimit && positionY[1] < -displacementLimit)
            //{
            //    direction = "斜向前";
            //    gesture = Gestures.LeanForward;
            //    Debug.LogWarning("斜向前");
            //}
            //else if (positionX[1] > displacementLimit && positionY[1] > displacementLimit)
            //{
            //    direction = "斜向后";
            //    gesture = Gestures.LeanBack;
            //    Debug.LogWarning("斜向后");
            //}
        }

        movement_end_check();
    }

    private void movement_end_check()
    {
        if (accelerationx[1] == 0)         //we count the number of acceleration samples that equals zero
        {
            countx++;
        }
        else
        {
            countx = 0;
        }
        if (countx >= 50)                     //if this number exceeds 25, we can assume that velocity is zero
        {
            velocityx[1] = 0;
            velocityx[0] = 0;
            positionX[1] = 0;
            positionX[0] = 0;
        }
        if (accelerationy[1] == 0)        //we do the same for the Y axis
        {
            county++;
        }
        else
        {
            county = 0;
        }
        if (county >= 50)
        {
            velocityy[1] = 0;
            velocityy[0] = 0;
            positionY[1] = 0;
            positionY[0] = 0;
        }
        if (positionX[1] == 0 && positionX[0] == 0 && positionY[1] == 0 && positionY[0] == 0)
        {
            direction = "";
        }
    }

    private float[] copyToArray()
    {
        List<float> temp = new List<float>();
        foreach(float[] f in dataBlock)
        {
            foreach(float x in f)
            {
                temp.Add(x);
            }
        }
        return temp.ToArray();
    }

    void OnDisable()
    {
        engine.Dispose();
    }
}
