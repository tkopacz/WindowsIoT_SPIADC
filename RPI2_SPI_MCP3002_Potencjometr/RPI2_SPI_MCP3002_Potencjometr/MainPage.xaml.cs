using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPI2_SPI_MCP3002_Potencjometr
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Podłączenia MCP3002 
        /*
        VSS - do GND
        VDD / VREF - do +5V
        */

        /*RaspBerry Pi2  Parameters*/
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */

        byte[] m_readBuffer = new byte[2]; /*this is defined to hold the output data*/
        byte[] m_writeBufferCH0 = new byte[] { 0x68, 0x00}; //0 1 10 1 000
        byte[] m_writeBufferCH1 = new byte[] { 0x70, 0x00}; //0 1 11 0 000
                                                            //
        int resCH0, resCH1;
        SpiDevice m_spiDev;
        DispatcherTimer m_timer;
        public MainPage() {
            this.InitializeComponent();
            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromMilliseconds(500); //100, 500
            m_timer.Tick += dispatcher_timer_Tick; ;
            InitSPIAndTimer();
        }

        private async void InitSPIAndTimer() {
            try {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 3000000;// 3200000;3000000;500000
                settings.Mode = SpiMode.Mode0; 

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                m_spiDev = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
                m_timer.Start();
            } catch (Exception) {
                uxBytes.Text = "NO SPI!";
                m_spiDev = null;
            }
        }
        private void dispatcher_timer_Tick(object sender, object e) {
            StringBuilder sb = new StringBuilder();
            m_spiDev.TransferFullDuplex(m_writeBufferCH0, m_readBuffer);
            resCH0 = convertToInt(m_readBuffer);
            sb.Append($"CH0: {m_readBuffer[0]:X2}|{m_readBuffer[1]:X2}:{resCH0:D4}");
            m_spiDev.TransferFullDuplex(m_writeBufferCH1, m_readBuffer);
            sb.Append($"  CH1: {m_readBuffer[0]:X2}|{m_readBuffer[1]:X2}:{resCH1:D4}");

            resCH1 = convertToInt(m_readBuffer);
            uxBytes.Text = sb.ToString();
            Debug.WriteLine(sb.ToString());
            uxChannel0.Value = resCH0; //Można - bo własciwy wątek (UI)
            uxChannel1.Value = resCH1;
        }
        public int convertToInt(byte[] data) {
            //10 bitowe wejście, czyli 2 + 8 bitów
            int result = data[0] & 0x03;
            result <<= 8;
            result += data[1];
            return result; //0 - 1023

        }
    }
}
