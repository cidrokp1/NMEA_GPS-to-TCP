using NmeaParser.Nmea;
using NmeaParser.Nmea.Gps;
using SharpKml.Base;
using SharpKml.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Point = SharpKml.Dom.Point;

namespace GPS_to_BMS
{
    public partial class Form1 : Form
    {
        public delegate void delUpdateUITextBox(String text, Color color);
        NetworkStream ns;
        StreamWriter writer;
        StreamReader reader;
        TcpClient svrSkt;
        bool TcpConnected = false;
        bool IsRunning = false;

        public Form1()
        {
            InitializeComponent();
        }


        public void UpdateUITextBox(string text, Color color)
        {
            string date = DateTime.Now + " - ";
            richTextBox1.AppendText(date);
            richTextBox1.Select(richTextBox1.Text.Length - date.Length, date.Length);
            richTextBox1.SelectionColor = Color.DarkBlue;
            richTextBox1.DeselectAll();
            richTextBox1.AppendText(text);
            richTextBox1.Select(richTextBox1.Text.Length - text.Length, text.Length);
            richTextBox1.SelectionColor = color;
            richTextBox1.DeselectAll();
        }



        public void button1_Click(object sender, EventArgs e)
        {
            try
            {
                svrSkt = new TcpClient(textBox2.Text, int.Parse(textBox3.Text));
                richTextBox1.Invoke(new delUpdateUITextBox(this.UpdateUITextBox), new object[] { "Connection accepted!\n ", Color.Green });
                ns = svrSkt.GetStream();
                reader = new StreamReader(ns);
                TcpConnected = true;
                
                string nmea = reader.ReadLine();
                richTextBox1.Invoke(new delUpdateUITextBox(this.UpdateUITextBox), new object[] { nmea+"\n ", Color.Green });
                
            }
            catch (Exception Ex)
            {
                richTextBox1.Invoke(new delUpdateUITextBox(this.UpdateUITextBox), new object[] { "Connection refused!\nError: "+Ex.Message, Color.Red });
            }
            

        }



        public void button2_Click(object sender, EventArgs e)
        {
            // Displays an OpenFileDialog so the user can select a Cursor.  
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "KML file|*.kml",
                Title = "Select a KML File to be Updated",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            try
            {
                // Show the Dialog.  
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string dbDir = openFileDialog1.FileName;
                    textBox1.Text = dbDir;
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }

        }



        public void button3_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                if (!IsRunning)
                {
                    button3.Text = "STOP";
                    try
                    {
                        printText("Initializing!\n ", Color.Green); 
                        Thread.Sleep(500);
                        svrSkt = new TcpClient(textBox2.Text, int.Parse(textBox3.Text));
                        printText("Connection accepted!\n ", Color.Green);
                        Thread.Sleep(500);
                        ns = svrSkt.GetStream();
                        reader = new StreamReader(ns);
                        TcpConnected = true;

                        string nmea = reader.ReadLine();
                        printText(nmea + "\n ", Color.Green);

                    }
                    catch (Exception Ex)
                    {
                        printText("Connection refused!\n Error: " + Ex.Message,Color.Red);
                        if (svrSkt!=null)
                        {
                            svrSkt.Close();
                        }
                        button3.Text = "START";
                    }
                    ThreadTCPlistener thread = new ThreadTCPlistener(this, svrSkt, reader, 4000, textBox1.Text);
                    Task.Run(() =>
                    {
                        // While asynchronously waiting for this to complete, 
                        // the thread is given back to the thread-pool
                        thread.updateGPScoordinates();
                    });
                   
                    
                }
                else {
                    button3.Text = "START";
                    printText("Stopping...\n ", Color.Green);
                    svrSkt.Close();
                    printText("Fully stopped\n ", Color.Green);
                }
            }
            else
            {
                string name;
                string message;
                StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
                Thread readThread = new Thread(PortChat.Read);
                SerialPort _serialPort;

                // Create a new SerialPort object with default settings.
                _serialPort = new SerialPort();

                // Allow the user to set the appropriate properties.
                _serialPort.PortName = PortChat.SetPortName("COM3");
                _serialPort.BaudRate = PortChat.SetPortBaudRate(9600);
                _serialPort.Parity = PortChat.SetPortParity(_serialPort.Parity);
                _serialPort.DataBits = PortChat.SetPortDataBits(_serialPort.DataBits);
                _serialPort.StopBits = PortChat.SetPortStopBits(_serialPort.StopBits);
                _serialPort.Handshake = PortChat.SetPortHandshake(_serialPort.Handshake);

                // Set the read/write timeouts
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;

                _serialPort.Open();
                bool _continue = true;
                readThread.Start();

                Console.Write("Name: ");
                name = Console.ReadLine();

                Console.WriteLine("Type QUIT to exit");

                while (_continue)
                {
                    message = Console.ReadLine();

                    if (stringComparer.Equals("quit", message))
                    {
                        _continue = false;
                    }
                    else
                    {
                        _serialPort.WriteLine(
                            String.Format("<{0}>: {1}", name, message));
                    }
                }
                readThread.Join();
                _serialPort.Close();
            }

           
        }


        internal void printText(string texto, Color color)
        {
            richTextBox1.Invoke(new delUpdateUITextBox(this.UpdateUITextBox), new object[] { texto, color });
            
        }


        internal void writeToKmlFile(string filePath, string lat, string lon, string alt)
        {
            try
            {
                string longitude = lon.Replace(",", ".");
                string longitudeInt = longitude.Substring(0, longitude.IndexOf(".") + 1);
                string longitudeDecimal = longitude.TrimStart(longitudeInt.ToArray()).Substring(0, 6);
                longitude = longitudeInt + longitudeDecimal;

                string latitude = lat.Replace(",", ".");
                string latitudeInt = latitude.Substring(0, latitude.IndexOf(".") + 1);
                string latitudeDecimal = latitude.TrimStart(latitudeInt.ToArray()).Substring(0, 6);
                latitude = latitudeInt + latitudeDecimal;
 

                //Here is the variable with which you assign a new value to the attribute
                string stringFile = System.Text.Encoding.UTF8.GetString(Properties.Resources.kmlFile);
                string finalString = stringFile.Replace("LONG", longitude);
                finalString = finalString.Replace("LAT", latitude);
                Console.WriteLine(finalString);
                System.IO.File.WriteAllText(filePath, finalString);
                
            }
            catch (Exception Ex)
            {
                throw;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                radioButton1.Checked = false;
                button2.Enabled = false;
                textBox1.Enabled = false;
                listBox1.Enabled = true;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton2.Checked = false;
                button2.Enabled = true;
                textBox1.Enabled = true;
                listBox1.Enabled = false;
            }
           

        }
    }





    public class ThreadTCPlistener
    {
        Form1 appForm;
        StreamReader reader;
        int period;
        TcpClient svrSkt;
        string filePath;

        public ThreadTCPlistener(Form1 form1, TcpClient svrSkt, StreamReader reader, int periodInMilli,string filePath)
        {
            this.reader = reader;
            this.period = periodInMilli;
            this.appForm = form1;
            this.svrSkt = svrSkt;
            this.filePath = filePath;
        }


        public void updateGPScoordinates()
        {
            try
            {
                NetworkStream ns = svrSkt.GetStream();
                reader = new StreamReader(ns);

                string texto="";
            
                while (true)
                {
                    string nmeaString = reader.ReadLine();
                    //string nmeaString = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47"; //debugging purposes!
                    if (nmeaString.Contains("$GPGGA") && !nmeaString.Split(',')[2].Equals(""))
                    {
                        Gga nmeaGpgga = (Gga)NmeaParser.Nmea.Gps.Gpgga.Parse(nmeaString);

                        texto = nmeaGpgga.ToString();
                        appForm.printText(texto,Color.Black);
                        Console.WriteLine(texto);

                        appForm.writeToKmlFile(filePath, nmeaGpgga.Latitude.ToString(),nmeaGpgga.Longitude.ToString(), nmeaGpgga.Altitude.ToString());
                        Thread.Sleep(period);
                    }
                }

            }
            
            catch (Exception Ex)
            {
                appForm.printText(Ex.ToString(), Color.Black);            }


            
        }

    }

}
