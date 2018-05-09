using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Timers;
using ZedGraph;
using System.Threading;
using Microsoft.Win32;
using System.IO;

namespace CSharpHW
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort = new SerialPort();//主机接口，用于与Arduino交互
        SerialPort virSerialPort = new SerialPort("COM1");//虚拟接口，用于验证modbus
        System.Timers.Timer timer = new System.Timers.Timer();//定时器
        //GraphPane myPane;//图
        PointPairList tempList = new PointPairList();//记录温度的值
        PointPairList lightList = new PointPairList();//记录光强的值
        LineItem tempCureve;//温度的线
        LineItem lightCureve;//光强的线
        string temp;//温度
        string light;//光强
        String diaLogFileAddress = null;
        bool isWriteDiaLog = false;
        DateTime dateTime = new DateTime();
        public MainWindow()
        {
            InitializeComponent();
            InitializeComboBox1(ComboBox1);
            InitializeComboBox2(ComboBox2);
            InitializeSerialPort(serialPort);
            InitializeVirSerailPort(virSerialPort);
            InitializeTimer(timer);
            InitializeMyPane();
            
        }

        private void InitializeMyPane()
        {
            this.zedGraphControl1.GraphPane.Title.Text = "温度光强折线图";

            this.zedGraphControl1.GraphPane.XAxis.Title.Text = "时间";

            this.zedGraphControl1.GraphPane.YAxis.Title.Text = "值";

            this.zedGraphControl1.GraphPane.XAxis.Type = ZedGraph.AxisType.DateAsOrdinal;
            double x = (double)new XDate(DateTime.Now);
            double temp_y = System.Convert.ToDouble(temp);
            double light_y = System.Convert.ToDouble(light);
            tempList.Add(x, temp_y);
            lightList.Add(x, light_y);
            tempCureve = zedGraphControl1.GraphPane.AddCurve("tempCurve", tempList, System.Drawing.Color.Red, SymbolType.Diamond);
            lightCureve = zedGraphControl1.GraphPane.AddCurve("lightCurve", lightList, System.Drawing.Color.Green, SymbolType.Diamond);
            this.zedGraphControl1.AxisChange();
            this.zedGraphControl1.Invoke(
                new Action(
                    delegate
                    {
                        zedGraphControl1.Refresh();
                    }));
        }

        private void InitializeTimer(System.Timers.Timer timer)
        {
            timer.Interval = 1000;
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            double x = (double)new XDate(DateTime.Now);
            double temp_y = System.Convert.ToDouble(temp);
            double light_y = System.Convert.ToDouble(light);
            tempList.Add(x, temp_y);
            lightList.Add(x, light_y);
            if (tempList.Count >= 10)
                tempList.RemoveAt(0);
            if (lightList.Count >= 10)
                lightList.RemoveAt(0);
            this.zedGraphControl1.AxisChange();
            this.zedGraphControl1.Invoke(
                new Action(
                    delegate
                    {
                        zedGraphControl1.Refresh();
                    }));

        }

        private void InitializeSerialPort(SerialPort serialPort)
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.PortName ="COM4";
                    serialPort.BaudRate = 9600;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = System.IO.Ports.StopBits.One;
                    serialPort.Open();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                serialPort.Close();
            }
            serialPort.DataReceived += new SerialDataReceivedEventHandler(Ser_DataReceived);
        }

        private void Ser_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialPort = (SerialPort)sender;
            temp = serialPort.ReadLine();
            light = serialPort.ReadLine();
            this.tempBox.Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        tempBox.Text = temp;
                    }));
            this.lightBox.Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        lightBox.Text = light;
                    }));
        }

        private void InitializeComboBox1(ComboBox comboBox1)
        {
            string[] arrPort = SerialPort.GetPortNames();
            foreach(string s in arrPort)
            {
                comboBox1.Items.Add(s);
            }
        }
        private void InitializeComboBox2(ComboBox comboBox2)
        {
            int[] bpsArray = { 9600, 19200, 38400, 57600 };
            foreach(int brate in bpsArray)
            {
                comboBox2.Items.Add(brate);
            }
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    //打开串口
        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            serialPort.Close();
            serialPort.PortName = ComboBox1.SelectedItem.ToString();
            if (ComboBox2.SelectedIndex == 1)
                serialPort.BaudRate = 9600;
            else if (ComboBox2.SelectedIndex == 2)
                serialPort.BaudRate = 19200;
            else if (ComboBox2.SelectedIndex == 3)
                serialPort.BaudRate = 38400;
            else if (ComboBox2.SelectedIndex == 4)
                serialPort.BaudRate = 57600;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;

            try
            {
                serialPort.Open();
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
                openOrCloseMessage.Text = ("Open " + ComboBox1.SelectedItem.ToString() + " successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    //关闭串口
        private void Button_Closes_Click(object sender, RoutedEventArgs e)
        {
            if(serialPort.IsOpen)
            {
                serialPort.Close();
                openOrCloseMessage.Text = ("Close " + ComboBox1.SelectedItem.ToString() + " successfully!");
            }
        }
    //打开灯的总闸
        private void ledControlButton_Click(object sender, RoutedEventArgs e)
        {
            redValue.Value = 255;
            yellowValue.Value = 255;
            greenValue.Value = 255;
            blueValue.Value = 255;
            whiteValue.Value = 255;
        }

     //灯的控制start
        private void redValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Argb.Color = Color.FromRgb((byte)(redValue.Value), (byte)greenValue.Value, (byte)blueValue.Value);
            byte c = (byte)'r';
            byte ledLIght =(byte)(redValue.Value);
            byte[] arr = { c, ledLIght };
            serialPort.Write(arr, 0, 1);
            Thread.Sleep(5);
            serialPort.Write(arr, 1,1);
            
        }

        private void greenValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Argb.Color = Color.FromRgb((byte)(redValue.Value), (byte)greenValue.Value, (byte)blueValue.Value);
            byte c = (byte)'g';
            byte ledLIght = (byte)(greenValue.Value);
            byte[] arr = { c, ledLIght };
            serialPort.Write(arr, 0, 1);
            Thread.Sleep(5);
            serialPort.Write(arr, 1, 1);
        }

        private void yellowValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte c = (byte)'y';
            byte ledLIght = (byte)(yellowValue.Value);
            byte[] arr = { c, ledLIght };
            serialPort.Write(arr, 0, 1);
            Thread.Sleep(5);
            serialPort.Write(arr, 1, 1);
        }

        private void blueValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Argb.Color = Color.FromRgb((byte)(redValue.Value), (byte)greenValue.Value, (byte)blueValue.Value);
            byte c = (byte)'b';
            byte ledLIght = (byte)(blueValue.Value);
            byte[] arr = { c, ledLIght };
            serialPort.Write(arr, 0, 1);
            Thread.Sleep(5);
            serialPort.Write(arr, 1, 1);
        }

        private void whiteValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte c = (byte)'w';
            byte ledLIght = (byte)(whiteValue.Value);
            byte[] arr = { c, ledLIght };
            serialPort.Write(arr, 0, 1);
            serialPort.Write(arr, 1, 1);
        }
 
        //灯的控制end

        //验证modbus rtu start
        //初始化虚拟串口
        private void InitializeVirSerailPort(SerialPort virSerialPort)
        {
            try
            {
                virSerialPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
                virSerialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //功能码0x03
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                byte slaveAddress = 0x0;
                byte functionCode = (byte)Convert.ToInt16(FunctionCode_Read.Text);
                short startAddress = 0;
                ushort numberOfPoints;
                if (SlaveAddress_Read.Text == "")
                {
                    throw (new Exception("未输入从机地址"));
                }
                else
                {
                    slaveAddress = (byte)Convert.ToInt16(SlaveAddress_Read.Text);
                }

                if (StartAddress_Read.Text == "")
                {
                    throw (new Exception("未输入寄存器起始地址"));
                }
                else
                {
                    startAddress = (short)Convert.ToInt16(StartAddress_Read.Text);
                }

                if (LengthOfData_Read.Text == "")
                {
                    throw (new Exception("未输入读取数据的长度"));
                }
                else
                {
                    numberOfPoints = (ushort)Convert.ToInt16(LengthOfData_Read.Text);
                }

                byte[] frame = this.ReadHoldingRegister(slaveAddress, functionCode, startAddress, numberOfPoints);
                //ContentOfSend.Items.Add(this.Display(frame));
                byte[] cacuDateCRC = new byte[2];
                if (!virSerialPort.IsOpen)
                {
                    virSerialPort.Open();
                }
                virSerialPort.Write(frame, 0, frame.Length);
                
                string contentOfSendStr;
                contentOfSendStr = "Read_Rx: " + this.Display(frame);
                if (isWriteDiaLog == true)
                {
                    System.IO.File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + contentOfSendStr + "\n");
                }
                ContentOfSend.Items.Add(contentOfSendStr);
                Thread.Sleep(100);
                
                if (virSerialPort.BytesToRead == 0)
                {
                    string sendException = "从机未回信，Read_Send 异常!";
                    ContentOfReceive.Items.Add(sendException);
                    if (isWriteDiaLog == true)
                    {
                        File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + sendException + "\n");
                    }
                }
                else
                {
                    byte[] buf = new byte[virSerialPort.BytesToRead];
                    int rs = virSerialPort.Read(buf, 0, buf.Length);
                    virSerialPort.DiscardInBuffer();
                    cacuDateCRC = CaculateCRC(buf);
                    DataCRC_Read.Text = this.Display(cacuDateCRC);
                    ReceiveCRC_Read.Text = string.Format("{0:X2} {1:X2}", buf[buf.Length - 2], buf[buf.Length - 1]);
                    string contentOfReceiveStr;
                    if (cacuDateCRC[0] != buf[buf.Length - 2] | cacuDateCRC[1] != buf[buf.Length - 1])
                    {
                        contentOfReceiveStr = "CRC不匹配";
                        if (isWriteDiaLog == true)
                        {
                            File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + "从机回信出错" + "\n");
                        }
                    }
                    else
                    {
                        contentOfReceiveStr = "Read_Tx: " + this.Display(buf);
                        if (isWriteDiaLog == true)
                        {
                            File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + contentOfReceiveStr + "\n");
                        }
                    }
                    ContentOfReceive.Items.Add(contentOfReceiveStr);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        //功能码0x10
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            try
            {
                byte slaveAddress = 0x1;
                byte functionCode = (byte)Convert.ToInt16(FunctionCode_Write.Text);
                short startAddress = 0;
                ushort numofRegisters;
                ushort[] writeArray = null;
                byte[] dataOfWriting = null;
               
                if (SlaveAddress_Write.Text == "")
                {
                    throw (new Exception("未输入从机地址"));
                }
                else
                {
                    slaveAddress = (byte)Convert.ToInt16(SlaveAddress_Write.Text);
                }
                if (StartAddress_Write.Text == "")
                {
                    throw (new Exception("未输入寄存器起始地址"));
                }
                else
                {
                    startAddress = (short)Convert.ToInt16(StartAddress_Write.Text);
                }
                if (NumOfRegisters_Write.Text == "")
                {
                    throw (new Exception("未输入所需寄存器数目"));
                }
                else
                {
                    numofRegisters = (ushort)Convert.ToInt16(NumOfRegisters_Write.Text);
                    writeArray = new ushort[numofRegisters];
                    for (int i = 0; i < numofRegisters; i++)
                        writeArray[i] = ushort.Parse(Data_Write.Text.Split(' ')[i]);
                    dataOfWriting = new byte[writeArray.Length * 2];
                    for (int i = 0, j = 0; i < dataOfWriting.Length;)
                    {
                        dataOfWriting[i] = (byte)((writeArray[j]) >> 8);
                        dataOfWriting[i + 1] = (byte)(writeArray[j]);
                        i += 2;
                        j++;
                    }
                }
                if (!virSerialPort.IsOpen)
                {
                    virSerialPort.Open();
                }
                byte[] frame = this.WriteHoldingRegister(slaveAddress, functionCode, startAddress, dataOfWriting);
                string contentOfSendStr;
                contentOfSendStr = "Write_Rx: " + this.Display(frame);
                if (isWriteDiaLog == true)
                {
                    System.IO.File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + contentOfSendStr + "\n");
                }
                ContentOfSend.Items.Add(contentOfSendStr);
                virSerialPort.Write(frame, 0, frame.Length);
                Thread.Sleep(100);
                if (virSerialPort.BytesToRead == 0)
                {
                    string sendException = "从机未回信，Read_Send 异常!";
                    ContentOfReceive.Items.Add(sendException);
                    if (isWriteDiaLog == true)
                    {
                        File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + sendException + "\n");
                    }
                }
                else
                {
                    byte[] cacuDateCRC = new byte[2];
                    byte[] buf = new byte[virSerialPort.BytesToRead];
                    int rs = virSerialPort.Read(buf, 0, buf.Length);
                    cacuDateCRC = CaculateCRC(buf);
                    DataCRC_Write.Text = this.Display(cacuDateCRC);
                    ReceiveCRC_Write.Text = string.Format("{0:X2} {1:X2}", buf[buf.Length - 2], buf[buf.Length - 1]);
                    string contentOfReceiveStr;
                    if (cacuDateCRC[0] != buf[buf.Length - 2] | cacuDateCRC[1] != buf[buf.Length - 1])
                    {
                        contentOfReceiveStr = "CRC不匹配";
                        if (isWriteDiaLog == true)
                        {
                            File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + "从机回信出错" + "\n");
                        }
                    }
                    else
                    {
                        contentOfReceiveStr = "Write_Tx: " + this.Display(buf);
                        if (isWriteDiaLog == true)
                        {
                            File.AppendAllText(diaLogFileAddress, DateTime.Now + ":" + dateTime.Millisecond + " : " + contentOfReceiveStr + "\n");
                        }
                    }
                    ContentOfReceive.Items.Add(contentOfReceiveStr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        //功能码0x03处理
        private byte[] ReadHoldingRegister(byte slaveAddress, byte functionCode, short startAddress, ushort numberOfPoints)
        {
            byte[] frame = new byte[8];//一共8bytes
            frame[0] = slaveAddress;//从机地址
            frame[1] = functionCode;//功能码
            frame[2] = (byte)(startAddress >> 8);//地址高位
            frame[3] = (byte)(startAddress);//地址低位
            frame[4] = (byte)(numberOfPoints >> 8);//数据长度高位
            frame[5] = (byte)(numberOfPoints);//数据长度低位
            byte[] crc = this.CaculateCRC(frame);//计算CRC
            frame[6] = crc[0];//CRC 低位
            frame[7] = crc[1];//crc 高位
            return frame;
        }
        //功能码0x16处理
        private byte[] WriteHoldingRegister(byte slaveAddress, byte functionCode, short startAddress, byte[] dataOfWriting)
        {
            ushort numberOfRegister = Convert.ToByte(dataOfWriting.Length / 2);
            ushort lengthOfWriting = Convert.ToByte(dataOfWriting.Length);
            byte[] frame = new byte[9 + dataOfWriting.Length];
            frame[0] = slaveAddress;//
            frame[1] = functionCode;//
            frame[2] = (byte)(startAddress >> 8);//
            frame[3] = (byte)(startAddress);//
            frame[4] = (byte)(numberOfRegister >> 8);//
            frame[5] = (byte)(numberOfRegister);//
            frame[6] = (byte)(lengthOfWriting);//
            for (int i = 0; i < dataOfWriting.Length; i++)//
                frame[i + 7] = dataOfWriting[i];
            byte[] crc = this.CaculateCRC(frame);
            frame[frame.Length - 2] = crc[0];
            frame[frame.Length - 1] = crc[1];
            return frame;
        }
        //CRC计算
        private byte[] CaculateCRC(byte[] frame)
        {
            byte[] result = new byte[2];
            ushort CRCFull = 0xFFFF;
            char CRCLSB;
            for (int i = 0; i < frame.Length - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ frame[i]);
                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (Char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);
                    if (CRCLSB == 1)
                    {
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                    }
                }
            }
            result[1] = (byte)((CRCFull >> 8) & 0xFF);
            result[0] = (byte)(CRCFull & 0xFF);
            return result;
        }
        //显示
        private string Display(byte[] frame)
        {
            string result = string.Empty;
            foreach (byte item in frame)
            {
                result += string.Format("{0:X2} ", item);
            }
            return result;
        }
        //开始日志
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "文本文件(*.txt)| *.txt| Json文件(*.Json)| *.Json| csv文件(*.csv)| *.csv";
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = "diaLog.csv";
            try
            {
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {

                    FileStream fileStream = (FileStream)saveFileDialog.OpenFile();
                }
                diaLogFileAddress = saveFileDialog.FileName.ToString();
                isWriteDiaLog = true;
                MessageBox.Show("日志文件设置成功,开始记录信息");
            }
            catch (Exception)
            {
                MessageBox.Show("保存失败");
            }
        }
        //关闭日志
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                isWriteDiaLog = false;
                MessageBox.Show("已停止写记录");
            }
            catch (Exception)
            {
                MessageBox.Show("停止写记录出现异常");
            }
        }
        //验证modbus rtu end
    }

}
