using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using BLE;
using UnityEngine;
using Unity.Barracuda;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEditor;

public class Test : MonoBehaviour
{
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
    private float sstatex = 5.000f;
    private float sstatey = 0.500f;
    private float sstatez = 9.865457f;
    private float windowlength = 1.5f;
    private float displacementLimit = 2.0f;

    private int sampleNum;
    private int countx;
    private int county;
    private int countz;

    private string direction = "";

    public Status stat;
    public Gestures gesture;
    public GameObject dataDisplay;
    public GameObject ConnectButton;
    public GameObject exampleModel;
    public GameObject GestureText;
    private Socket socket;
    private IPAddress ip;
    private IPEndPoint endPoint;
    private bool isConnect =true;
    private TextMeshPro data;
    private TextMeshPro gest;
    private ButtonConfigHelper configHelper;
    private List<int> rawData;
    private List<float[]> dataBlock;
    private int[] deviceData;

    private Model model;
    private IWorker engine;
    private Tensor output;
    public NNModel modelAsset;

    public int accelLSB;
    public int angularLSB;
    public int dataBlockSize;
    public int dataLength;

    

    // Start is called before the first frame update
    void Start()
    {
        stat = Status.NoTouch;
        configHelper = ConnectButton.GetComponent<ButtonConfigHelper>();
        data = dataDisplay.GetComponent<TextMeshPro>();
        gest = GestureText.GetComponent<TextMeshPro>();
        //socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ip = IPAddress.Parse("192.168.1.101");
        endPoint = new IPEndPoint(ip, 3000);
        rawData = new List<int>();
        dataBlock = new List<float[]>();
        model = ModelLoader.Load(modelAsset);
        engine = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, model);
    }

    // Update is called once per frame
    void Update()
    {
        if (socket != null && deviceData != null)
        {
            if(socket.Connected && deviceData.Length > 0)
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

    public void Close()
    {
        #if UNITY_EDITOR    //在编辑器模式下
                EditorApplication.isPlaying = false;
        #else
                                Application.Quit();
        #endif
    }

    public void ConnectServer()
    {
        //this.Close();
        if (isConnect)
        {
            configHelper.MainLabelText = "DisConnect";
            exampleModel.SetActive(true);
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();//创建连接参数对象
                args.RemoteEndPoint = endPoint;
                args.Completed += OnConnectedCompleted;//添加连接创建成功监听
                socket.ConnectAsync(args); //异步创建连接
            }
            catch (Exception e)
            {
                Debug.Log("服务器连接异常:" + e);
            }
            isConnect = false;
        }
        else
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;
            exampleModel.SetActive(false);
            configHelper.MainLabelText = "Connect";
            //isConnected = false;
            isConnect = true;
        }
    }

    private void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
    {
        try
        {   ///连接创建成功监听处理
            if (args.SocketError != SocketError.Success)
            {
                //通知上层连接失败
                //isConnected = false;
                Debug.Log("连接失败");
            }
            else
            {
                Debug.Log("网络连接成功线程：" + Thread.CurrentThread.ManagedThreadId.ToString());
                //通知上层连接创建成功
                Debug.Log("连接成功");
                //isConnected = true;
                StartReceiveMessage();//启动接收消息

            }
        }
        catch (Exception e)
        {
            Debug.Log("开启接收数据异常" + e);
        }

    }

    private void StartReceiveMessage()
    {
        Debug.Log("开始接收数据");
        //启动接收消息
        SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
        //设置接收消息的缓存大小，正式项目中可以放在配置 文件中
        byte[] buffer = new byte[20];
        //设置接收缓存
        receiveArgs.SetBuffer(buffer, 0, buffer.Length);
        receiveArgs.RemoteEndPoint = endPoint;
        receiveArgs.Completed += OnReceiveCompleted; //接收成功
        socket.ReceiveAsync(receiveArgs);//开始异步接收监听
    }

    public void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
    {
        try
        {
            Debug.Log("网络接收成功线程：" + Thread.CurrentThread.ManagedThreadId.ToString());

            if (args.SocketError == SocketError.Success && args.BytesTransferred > 0)
            {
                //创建读取数据的缓存
                byte[] bytes = new byte[args.BytesTransferred];
                //将数据复制到缓存中
                Buffer.BlockCopy(args.Buffer, 0, bytes, 0, bytes.Length);
                //Debug.Log(string.Join("," ,bytes));
                foreach (byte x in bytes)
                {
                    //Debug.LogWarning(x.ToString("X2"));
                    rawData.Add(Convert.ToSByte(x.ToString("X2"), 16));
                }
                //toUIThread(rawData.ToArray());
                deviceData = rawData.ToArray();
                rawData.Clear();
                Debug.Log(string.Join(",", bytes));
                Debug.Log(string.Join(",", deviceData));
                //bytes = null;

                //接收数据成功，调上层处理接收数据的事件
            }
            if (args.SocketError == SocketError.Success) 
            {
                StartReceiveMessage();
            }
            
        }
        catch (Exception e)
        {
            Debug.Log("接收数据异常：" + e);
        }
    }

    private void InputTransform()
    {
        List<float> derData = new List<float>();
        //0-5转成加速度数据，全部截断只保留整数部分
        for (int i = 0; i < 6; i += 2)
        {
            double temp = ((deviceData[i + 1] << 8) | deviceData[i]) * 9.8 / 16384;
            derData.Add((float)Math.Round(temp, 3));
        }
        //6-11转成角速度数据，全部截断只保留整数部分
        for (int i = 6; i < 12; i += 2)
        {
            double temp = ((deviceData[i + 1] << 8) | deviceData[i]) * (Math.PI / 180) / 131;
            derData.Add((float)Math.Round(temp, 3));
        }
        der = derData.ToArray();
        //Debug.Log(string.Join(",", der));
        derData.Clear();
        if(((int)der[0]) != 0)
        {
            GetGestures();
        }
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

    private Status GetStatus()
    {
        Status status = Status.NoTouch;
        int index = output.ArgMax()[0];
        //Debug.LogWarning(index.ToString());
        switch (index)
        {
            case 0:
                status = Status.NoTouch;
                data.text = "NoTouch";
                Debug.LogWarning("NoTouch");
                //Debug.Log("Horizontial");
                break;
            case 1:
                status = Status.Touch;
                data.text = "Touch";
                Debug.LogWarning("Touch");
                //Debug.Log("Vertical");
                break;
        }
        return status;
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

    private float[] copyToArray()
    {
        List<float> temp = new List<float>();
        foreach (float[] f in dataBlock)
        {
            foreach (float x in f)
            {
                temp.Add(x);
            }
        }
        return temp.ToArray();
    }


    private void GetGestures()
    {
        //float temp = der[2];
        //if(der[2] > 9.8f)
        //{
        //    temp = 9.8f;
        //}
        //float pitch = (float) (90.0f - Math.Acos(temp / 9.8));

        //float gx = (float) (9.8 * Math.Cos(pitch / 180 * Math.PI));

        //Debug.LogError(gx+"");

        //Gestures g = Gestures.None;
        accelerationx[1] = der[0];
        accelerationy[1] = der[1];
        //Debug.LogWarning("accelerationx:" + der[0] + "accelerationy:" + der[1]);

        accelerationx[1] -= sstatex;
        accelerationy[1] -= sstatey;

        //给定一个窗口大小，这里窗口为2，如果加速度小于这个区间这表明没有移动
        if ((accelerationx[1] <= windowlength) && (accelerationx[1] >= -windowlength))
        { accelerationx[1] = 0; }

        if ((accelerationy[1] <= windowlength) && (accelerationy[1] >= -windowlength))
        { accelerationy[1] = 0; }

        Debug.LogWarning("accelerationx:" + accelerationx[1] + "accelerationy:" + accelerationy[1]);

        //first X integration:
        velocityx[1] = velocityx[0] + accelerationx[0] + ((accelerationx[1] - accelerationx[0]) / 2);

        //second X integration:
        //positionX[1] = positionX[0] + velocityx[0] + ((velocityx[1] - velocityx[0]) / 2);
        //positionX[1] = velocityx[0] + ((velocityx[1] - velocityx[0]) / 2);

        //first Y integration:
        velocityy[1] = velocityy[0] + accelerationy[0] + ((accelerationy[1] - accelerationy[0]) / 2);

        //second Y integration:
        //positionY[1] = positionY[0] + velocityy[0] + ((velocityy[1] - velocityy[0]) / 2);
        //positionY[1] = velocityy[0] + ((velocityy[1] - velocityy[0]) / 2);

        accelerationx[0] = accelerationx[1];
        accelerationy[0] = accelerationy[1];

        velocityx[0] = velocityx[1];
        velocityy[0] = velocityy[1];

        //positionX[0] = positionX[1];
        //positionY[0] = positionY[1];

        //Debug.LogWarning("velX:" + velocityx[1] + "velY:" + velocityy[1]);
        if (direction == "")
        {
            if (velocityy[1] < -displacementLimit && accelerationx[1] == 0 && velocityx[1] == 0)
            {
                direction = "右";
                gesture = Gestures.Right;
                gest.text = "Right";
                Debug.LogError("右");
            }
            else if (velocityy[1] > displacementLimit && accelerationx[1] == 0 && velocityx[1] == 0)
            {
                direction = "左";
                gesture = Gestures.Left;
                gest.text = "Left";
                Debug.LogError("左");
            }
            else if (velocityx[1] < -displacementLimit && accelerationy[1] == 0 && velocityy[1] == 0)
            {
                direction = "前";
                gesture = Gestures.Forward;
                gest.text = "Forward";
                Debug.LogError("前");
            }
            else if (velocityx[1] > displacementLimit && accelerationy[1] == 0 && velocityy[1] == 0)
            {
                direction = "后";
                gesture = Gestures.Back;
                gest.text = "Back";
                Debug.LogError("后");
            }
        }

        movement_end_check();
    }

    private void movement_end_check()
    {
        //if (accelerationx[1] == 0 && accelerationy[1] == 0)
        //{
        //    countx++;
        //}
        //else
        //{
        //    countx = 0;
        //}
        //if (countx == 50)
        //{
        //    direction = "";
        //}
        if (accelerationx[1] == 0 && accelerationy[1] == 0)         //we count the number of acceleration samples that equals zero
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
            velocityy[1] = 0;
            velocityy[0] = 0;
        }
        //if (accelerationy[1] == 0)        //we do the same for the Y axis
        //{
        //    county++;
        //}
        //else
        //{
        //    county = 0;
        //}
        //if (county >= 50)
        //{
        //    velocityy[1] = 0;
        //    velocityy[0] = 0;
        //}
        //if (accelerationx[1] == 0)         //we count the number of acceleration samples that equals zero
        //{ countx++; }
        //else { countx = 0; }
        //if (countx >= 50)                     //if this number exceeds 25, we can assume that velocity is zero
        //{
        //    velocityx[1] = 0;
        //    velocityx[0] = 0;
        //}
        //if (accelerationy[1] == 0)        //we do the same for the Y axis
        //{ county++; }
        //else { county = 0; }
        //if (county >= 50)
        //{
        //    velocityy[1] = 0;
        //    velocityy[0] = 0;
        //}
        if (velocityx[1] == 0 && velocityx[0] == 0 && velocityy[1] == 0 && velocityy[0] == 0)
        {
            direction = "";
        }
    }

    private void OnDestroy()
    {
        //isConnected = false;
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
        socket = null;
    }

}
