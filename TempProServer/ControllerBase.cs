using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualBasic;

namespace TempProServer
{
    /// <summary>
    /// This code is decompiled from TempPro v2.0 and remains almost as-is,
    /// only some unavoidable refactoring was done.
    /// Unused code is commented out.
    /// </summary>
    public abstract class ControllerBase
    {
        protected bool EZ_Zone;

        // Make constants actually constant
        protected const short FT_OK = 0;
        protected const short FT_IO_ERROR = 4;
        protected const short FT_BITS_8 = 8;
        protected const short FT_STOP_BITS_1 = 0;
        protected const short FT_PARITY_NONE = 0;
        protected const short FT_FLOW_NONE = 0;
        protected const short FT_PURGE_RX = 1;
        protected const short FT_PURGE_TX = 2;
        protected const string LineEnding = "\n\r";
        // Remove unnecessary variables from class scope
        protected int lngUSBHandle;
        protected int lngBytesRead;
        protected int lngTotalBytesRead;
        protected bool flFailed;
        protected bool flTimedout;
        protected bool flFatalError;
        protected int ftStatus;
        protected int lngTotalBytesReceived;
        protected int SoftwareNum;
        protected bool commok;
        protected bool bDegF;
        protected double sngSetPointHighLimit;


        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_Open(short intDeviceNumber, ref int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_Close(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_Read(int lngUSBHandle, byte[] lpszBuffer, int lngBufferSize, ref int lngBytesReturned);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_Write(int lngUSBHandle, byte[] lpszBuffer, int lngBufferSize, ref int lngBytesWritten);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetBaudRate(int lngUSBHandle, int lngBaudRate);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetDataCharacteristics(int lngUSBHandle, byte byWordLength, byte byStopBits, byte byParity);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetFlowControl(int lngUSBHandle, short intFlowControl, byte byXonChar, byte byXoffChar);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_Purge(int lngUSBHandle, int lngMask);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_GetQueueStatus(int lngUSBHandle, ref int lngRxBytes);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetTimeouts(int lngUSBHandle, int lngReadTimeout, int lngWriteTimeout);
        /*
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_ResetDevice(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetDtr(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_ClrDtr(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetRts(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_ClrRts(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_GetModemStatus(int lngUSBHandle, ref int lngModemStatus);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_GetStatus(int lngUSBHandle, ref int lngRxBytes, ref int lngTxBytes, ref int lngEventsDWord);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetBreakOn(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetBreakOff(int lngUSBHandle);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_GetEventStatus(int lngUSBHandle, ref int lngEventsDWord);
        [DllImport("FTD2XX.DLL", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        protected static extern int FT_SetChars(int lngUSBHandle, byte byEventChar, byte byEventCharEnabled, byte byErrorChar, byte byErrorCharEnabled);
        */

        protected bool OpenUSBPort()
        {
            flFailed = true;
            ftStatus = FT_GetQueueStatus(lngUSBHandle, ref lngTotalBytesReceived);
            string text;
            bool result;
            if (((ftStatus != FT_OK) || (ftStatus == FT_IO_ERROR)) && FT_Open(0, ref lngUSBHandle) != FT_OK)
            {
                text = "USB Open Failed";
            }
            else if (FT_SetBaudRate(lngUSBHandle, 9600) != FT_OK)
            {
                text = "Set Transfer Rate Failed";
            }
            else if (checked(FT_SetDataCharacteristics(lngUSBHandle, (byte)FT_BITS_8, (byte)FT_STOP_BITS_1, (byte)FT_PARITY_NONE)) != FT_OK)
            {
                text = "SetDataCharacteristics Failed";
            }
            else if (FT_SetFlowControl(lngUSBHandle, FT_FLOW_NONE, 0, 0) != FT_OK)
            {
                text = "SetFlowControl Failed";
            }
            else
            {
                if (FT_SetTimeouts(lngUSBHandle, 1000, 1000) == FT_OK)
                {
                    commok = true;
                    result = true;
                    return result;
                }
                text = "SetTimeout Failed";
            }
            string text2 = "There is a problem with the USB port." + LineEnding;
            text2 = text2 + "The problem could be: " + LineEnding;
            text2 = text2 + text + LineEnding;
            Console.WriteLine($"TempPro USB problem: {text2}");
            commok = false;
            result = false;
            if (FT_Close(lngUSBHandle) != FT_OK)
            {
                if (commok)
                {
                    text2 = "There is a problem with the USB port." + LineEnding;
                    text2 = text2 + "The problem could be: " + LineEnding;
                    text2 = text2 + "Could not close USB port" + LineEnding;
                    Console.WriteLine($"TempPro USB problem: {text2}");
                }
                commok = false;
                result = false;
            }
            if (flFailed)
            {
                if (commok)
                {
                    text2 = "There is a problem with the USB port." + LineEnding;
                    text2 = text2 + "The problem could be: " + LineEnding;
                    text2 = text2 + "Could not close USB port" + LineEnding;
                    Console.WriteLine($"TempPro USB problem: {text2}");
                }
                commok = false;
                result = false;
            }
            return result;
        }

        protected bool CloseUSBPort()
        {
            if (FT_Close(lngUSBHandle) != FT_OK)
            {
                if (commok)
                {
                    string text = "There is a problem with the USB port." + LineEnding;
                    text = text + "The problem could be: " + LineEnding;
                    text = text + "Could not close USB port" + LineEnding;
                    Console.WriteLine($"TempPRO USB Port Problem: ${text}");
                }
                commok = false;
            }
            return true;
        }

        protected void SENDSTRINGusb(ref byte[] theData)
        {
            checked
            {
                if (commok)
                {
                    PurgeUSBBuffers();
                    int lngBytesWritten = 0;
                    string text;
                    if (FT_Write(lngUSBHandle, theData, Information.UBound(theData) + 1, ref lngBytesWritten) != FT_OK)
                    {
                        text = "Write Failed";
                    }
                    else
                    {
                        short n = 200;
                        delay(n);
                        lngTotalBytesReceived = 0;
                        short num = default(short);
                        do
                        {
                            ftStatus = FT_GetQueueStatus(lngUSBHandle, ref lngTotalBytesReceived);
                            if ((ftStatus != FT_OK) | (ftStatus == FT_IO_ERROR))
                            {
                                text = "Get USB read status Failed from send string";
                                num++;
                                continue;
                            }
                            flTimedout = false;
                            flFatalError = false;
                            lngTotalBytesRead = 0;
                            theData = (byte[])CompatApi.CopyArray(theData, new byte[lngTotalBytesReceived - 1 + 1]);
                            do
                            {
                                lngBytesRead = 0;
                                ftStatus = FT_Read(lngUSBHandle, theData, lngTotalBytesReceived, ref lngBytesRead);
                                if ((ftStatus == FT_OK) | (ftStatus == FT_IO_ERROR))
                                {
                                    if (lngBytesRead > 0)
                                    {
                                        lngTotalBytesRead += lngBytesRead;
                                    }
                                    else
                                    {
                                        flTimedout = true;
                                    }
                                }
                                else
                                {
                                    flFatalError = true;
                                }
                            }
                            while (!(flTimedout | flFatalError | (lngBytesRead == lngTotalBytesReceived)));
                            theData = (byte[])CompatApi.CopyArray(theData, new byte[lngTotalBytesReceived - 1 + 1]);
                            int num2 = lngTotalBytesReceived - 1;
                            int num3 = 0;
                            while (true)
                            {
                                int num4 = num3;
                                int num5 = num2;
                                if (num4 > num5)
                                {
                                    break;
                                }
                                num3++;
                            }
                            PurgeUSBBuffers();
                            return;
                        }
                        while (num < 5);
                    }
                    if (commok)
                    {
                        string text2 = "There is a problem with the USB port." + LineEnding;
                        text2 = text2 + "The problem could be: " + LineEnding;
                        text2 = text2 + text + LineEnding;
                        Console.WriteLine($"TempPro USB problem: {text2}");
                    }
                    commok = false;
                    if (FT_Close(lngUSBHandle) != FT_OK)
                    {
                        if (commok)
                        {
                            string text2 = "There is a problem with the USB port." + LineEnding;
                            text2 = text2 + "The problem could be: " + LineEnding;
                            text2 = text2 + "Could not close USB port" + LineEnding;
                            Console.WriteLine($"TempPro USB problem: {text2}");
                        }
                        commok = false;
                    }
                    if (flFailed)
                    {
                        if (commok)
                        {
                            string text2 = "There is a problem with the USB port." + LineEnding;
                            text2 = text2 + "The problem could be: " + LineEnding;
                            text2 = text2 + "Could not close USB port" + LineEnding;
                            Console.WriteLine($"TempPro USB problem: {text2}");
                        }
                        commok = false;
                    }
                }
                else
                {
                    short n = 20;
                    delay(n);
                }
            }
        }

        protected static void delay(short n)
        {
            Thread.Sleep(n);
        }

        private void PurgeUSBBuffers()
        {
            ftStatus = FT_Purge(lngUSBHandle, FT_PURGE_RX);
            string text;
            if (ftStatus != FT_OK)
            {
                text = "USB Rx purge error";
            }
            else
            {
                ftStatus = FT_Purge(lngUSBHandle, FT_PURGE_TX);
                if (ftStatus == FT_OK)
                {
                    return;
                }
                text = "USB Tx purge error";
            }
            if (commok)
            {
                string text2 = "There is a problem with the USB port." + LineEnding;
                text2 = text2 + "The problem could be: " + LineEnding;
                text2 = text2 + text + LineEnding;
                Console.WriteLine($"TempPro USB problem: {text2}");
            }
            commok = false;
        }

        protected Tuple<bool, string> USBStatusCheck(Controller currUSBObject)
        {
            string text2 = "OK";
            currUSBObject.ftStatus = FT_GetQueueStatus(currUSBObject.lngUSBHandle, ref currUSBObject.lngTotalBytesReceived);
            if ((currUSBObject.ftStatus != FT_OK) | (currUSBObject.ftStatus == FT_IO_ERROR))
            {
                string text = "USB read status Failed";
                text2 = "There is a problem with the USB port." + LineEnding;
                text2 = text2 + "The problem could be: " + LineEnding;
                text2 = text2 + "The cable was disconnected or " + LineEnding;
                text2 = text2 + "the control unit was shut off." + LineEnding;
                text2 = text2 + text + LineEnding;
                text2 = text2 + LineEnding + "You will need to re-initialize the controller." + LineEnding;
                Console.WriteLine($"TempPro USB problem: {text2}");
                commok = false;
                currUSBObject.lngUSBHandle = 0;
            }
            if (currUSBObject.lngUSBHandle == 0)
            {
                return new Tuple<bool, string>(false, text2);
            }
            return new Tuple<bool, string>(true, text2);
        }

        protected int CRC16A(ref byte[] Buffer)
        {
            int num = 65535;
            checked
            {
                int num2 = Information.UBound(Buffer) - 1;
                int num3 = 0;
                while (true)
                {
                    int num4 = num3;
                    int num5 = num2;
                    if (num4 > num5)
                    {
                        break;
                    }
                    int num6 = Buffer[num3];
                    num ^= num6;
                    short num7 = 0;
                    short num8;
                    short num9;
                    do
                    {
                        unchecked
                        {
                            if (((uint)num & 1u) != 0)
                            {
                                num /= 2;
                                num ^= 0xA001;
                            }
                            else
                            {
                                num /= 2;
                            }
                        }
                        num7 = (short)unchecked(num7 + 1);
                        num8 = num7;
                        num9 = 7;
                    }
                    while (num8 <= num9);
                    num3++;
                }
                return num;
            }
        }
        /*
        protected int CRC32A(ref byte[] Buffer)
        {
            int num = 65535;
            checked
            {
                int num2 = Information.UBound(Buffer) - 1;
                int num3 = 0;
                while (true)
                {
                    int num4 = num3;
                    int num5 = num2;
                    if (num4 > num5)
                    {
                        break;
                    }
                    int num6 = Buffer[num3];
                    num ^= num6;
                    short num7 = 0;
                    short num8;
                    short num9;
                    do
                    {
                        unchecked
                        {
                            if (((uint)num & 1u) != 0)
                            {
                                num /= 2;
                                num ^= 0xA001;
                            }
                            else
                            {
                                num /= 2;
                            }
                        }
                        num7 = (short)unchecked(num7 + 1);
                        num8 = num7;
                        num9 = 15;
                    }
                    while (num8 <= num9);
                    num3++;
                }
                return num;
            }
        }
        */

        protected int QueryController(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData = new byte[8]
                {
                    (byte)intDevAddr,
                    3,
                    0,
                    8,
                    0,
                    2,
                    69,
                    201
                };
                currUSBObject.SENDSTRINGusb(ref theData);
                if (currUSBObject.lngTotalBytesRead < 14)
                {
                    throw new InvalidDataException();
                }
                else
                {
                    byte b = theData[11];
                    byte b2 = theData[12];
                    byte b3 = theData[13];
                    byte b4 = theData[14];
                    byte[] array = new byte[4] { b, b2, b3, b4 };
                    Array.Reverse(array);
                    SoftwareNum = BitConverter.ToInt32(array, 0);
                }
                return SoftwareNum;
            }
        }

        protected double GetProcessValue(Controller currUSBObject, int intDevAddr, bool bUnitsF)
        {
            checked
            {
                double num;
                if (EZ_Zone)
                {
                    byte[] theData = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        1,
                        104,
                        0,
                        2,
                        68,
                        43
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    if (currUSBObject.lngTotalBytesRead < 14)
                    {
                        num = double.NaN;
                    }
                    else
                    {
                        byte b = theData[11];
                        byte b2 = theData[12];
                        byte b3 = theData[13];
                        byte b4 = theData[14];
                        byte[] array = new byte[4] { b, b2, b3, b4 };
                        Array.Reverse(array);
                        num = BitConverter.ToSingle(array, 0);
                    }
                }
                else
                {
                    byte[] theData2 = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        0,
                        20,
                        0,
                        2,
                        132,
                        15
                    };
                    currUSBObject.SENDSTRINGusb(ref theData2);
                    num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData2[11] << 24) + (theData2[12] << 16) + (theData2[13] << 8) + theData2[14]) / 1000.0) : double.NaN);
                }
                if (!bUnitsF && !EZ_Zone)
                {
                    return (num - 32.0) * 5.0 / 9.0;
                }
                return num;
            }
        }

        protected double GetSetpoint(Controller currUSBObject, int intDevAddr, bool bUnitsF)
        {
            checked
            {
                double num;
                if (EZ_Zone)
                {
                    byte[] theData = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        10,
                        92,
                        0,
                        2,
                        7,
                        193
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    if (currUSBObject.lngTotalBytesRead < 14)
                    {
                        num = double.NaN;
                    }
                    else
                    {
                        byte b = theData[11];
                        byte b2 = theData[12];
                        byte b3 = theData[13];
                        byte b4 = theData[14];
                        byte[] array = new byte[4] { b, b2, b3, b4 };
                        Array.Reverse(array);
                        num = BitConverter.ToSingle(array, 0);
                    }
                }
                else
                {
                    byte[] theData2 = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        0,
                        27,
                        0,
                        2,
                        180,
                        12
                    };
                    currUSBObject.SENDSTRINGusb(ref theData2);
                    num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData2[11] << 24) + (theData2[12] << 16) + (theData2[13] << 8) + theData2[14]) / 1000.0) : double.NaN);
                }
                if (!bUnitsF && !EZ_Zone)
                {
                    return (num - 32.0) * 5.0 / 9.0;
                }
                return num;
            }
        }

        protected double GetRTDSetpointHighLimit(Controller currUSBObject, int intDevAddr, bool bUnitsF)
        {
            checked
            {
                double num;
                if (EZ_Zone)
                {
                    byte[] theData = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        10,
                        86,
                        0,
                        2,
                        39,
                        195
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    if (currUSBObject.lngTotalBytesRead < 14)
                    {
                        num = double.NaN;
                    }
                    else
                    {
                        byte b = theData[11];
                        byte b2 = theData[12];
                        byte b3 = theData[13];
                        byte b4 = theData[14];
                        byte[] array = new byte[4] { b, b2, b3, b4 };
                        Array.Reverse(array);
                        num = BitConverter.ToSingle(array, 0);
                    }
                }
                else
                {
                    byte[] theData2 = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        0,
                        246,
                        0,
                        2,
                        36,
                        57
                    };
                    currUSBObject.SENDSTRINGusb(ref theData2);
                    num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData2[11] << 24) + (theData2[12] << 16) + (theData2[13] << 8) + theData2[14]) / 1000.0) : double.NaN);
                }
                if (!bUnitsF && !EZ_Zone)
                {
                    return (num - 32.0) * 5.0 / 9.0;
                }
                return num;
            }
        }

        protected double GetTCSetpointHighLimit(Controller currUSBObject, int intDevAddr, bool bUnitsF)
        {
            checked
            {
                double num;
                if (EZ_Zone)
                {
                    byte[] theData = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        10,
                        86,
                        0,
                        2,
                        39,
                        195
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    if (currUSBObject.lngTotalBytesRead < 14)
                    {
                        num = double.NaN;
                    }
                    else
                    {
                        byte b = theData[11];
                        byte b2 = theData[12];
                        byte b3 = theData[13];
                        byte b4 = theData[14];
                        byte[] array = new byte[4] { b, b2, b3, b4 };
                        Array.Reverse(array);
                        num = BitConverter.ToSingle(array, 0);
                    }
                }
                else
                {
                    byte[] theData2 = new byte[8]
                    {
                        (byte)intDevAddr,
                        3,
                        0,
                        242,
                        0,
                        2,
                        101,
                        248
                    };
                    currUSBObject.SENDSTRINGusb(ref theData2);
                    num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData2[11] << 24) + (theData2[12] << 16) + (theData2[13] << 8) + theData2[14]) / 1000.0) : double.NaN);
                }
                if (!bUnitsF && !EZ_Zone)
                {
                    return (num - 32.0) * 5.0 / 9.0;
                }
                return num;
            }
        }

        protected float SetTempUnitsC(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData = ((!EZ_Zone) ? new byte[8]
                {
                    (byte)intDevAddr,
                    6,
                    0,
                    40,
                    0,
                    1,
                    200,
                    2
                } : new byte[13]
                {
                    (byte)intDevAddr,
                    16,
                    9,
                    4,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    15,
                    216,
                    8
                });
                currUSBObject.SENDSTRINGusb(ref theData);
                double num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData[11] << 24) + (theData[12] << 16) + (theData[13] << 8) + theData[14]) / 1000.0) : 25.0);
                return (float)num;
            }
        }

        protected float SetTempUnitsF(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData = ((!EZ_Zone) ? new byte[8]
                {
                    (byte)intDevAddr,
                    6,
                    0,
                    40,
                    0,
                    0,
                    9,
                    194
                } : new byte[13]
                {
                    (byte)intDevAddr,
                    16,
                    9,
                    4,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    30,
                    24,
                    4
                });
                currUSBObject.SENDSTRINGusb(ref theData);
                double num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData[11] << 24) + (theData[12] << 16) + (theData[13] << 8) + theData[14]) / 1000.0) : 25.0);
                return (float)num;
            }
        }

        protected bool SetEnableRampControl(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    10,
                    106,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    13,
                    202,
                    157
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[8]
                {
                (byte)intDevAddr,
                6,
                1,
                10,
                0,
                2,
                255,
                255
                };
                byte[] destinationArray = new byte[6];
                Array.Copy(theData, destinationArray, 5);
                theData[6] = 41;
                theData[7] = 245;
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetRampRateDegPerMin(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    10,
                    108,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    57,
                    75,
                    96
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[8]
                {
                (byte)intDevAddr,
                6,
                1,
                11,
                0,
                1,
                255,
                255
                };
                byte[] destinationArray = new byte[6];
                Array.Copy(theData, destinationArray, 5);
                theData[6] = 56;
                theData[7] = 52;
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetDisableRampControl(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    10,
                    106,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    62,
                    138,
                    136
                    };
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[8]
                {
                (byte)intDevAddr,
                6,
                1,
                10,
                0,
                0,
                255,
                255
                };
                byte[] destinationArray = new byte[6];
                Array.Copy(theData, destinationArray, 5);
                theData[6] = 168;
                theData[7] = 52;
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetSetPoint(Controller currUSBObject, int intDevAddr, double setPtValue, bool bUnitsF)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    //Removed string parsing from here
                    byte[] bytes = BitConverter.GetBytes(setPtValue);
                    Array.Reverse(bytes);
                    b = bytes[0];
                    b2 = bytes[1];
                    b3 = bytes[2];
                    b4 = bytes[3];
                    theData = new byte[13]
                    {
                        1, 16, 10, 80, 0, 2, 4, b, b2, b3,
                        b4, 255, 255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                    (byte)intDevAddr,
                    16,
                    0,
                    27,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    0,
                    255,
                    255
                };
                if (!bUnitsF)
                {
                    setPtValue = setPtValue * 9.0 / 5.0 + 32.0;
                }
                setPtValue *= 1000.0;
                theData[7] = (byte)unchecked((checked((long)Math.Round(setPtValue)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(setPtValue)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(setPtValue)) >> 8) % 256);
                theData[10] = (byte)Math.Round(setPtValue % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = CRC16A(ref Buffer2);
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetRampRate(Controller currUSBObject, int intDevAddr, double rampRateValue)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    //Remove string parsing from here
                    byte[] bytes = BitConverter.GetBytes(rampRateValue);
                    Array.Reverse(bytes);
                    b = bytes[0];
                    b2 = bytes[1];
                    b3 = bytes[2];
                    b4 = bytes[3];
                    theData = new byte[13]
                    {
                        (byte)intDevAddr,
                        16,
                        10,
                        112,
                        0,
                        2,
                        4,
                        b,
                        b2,
                        b3,
                        b4,
                        255,
                        255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                    (byte)intDevAddr,
                    16,
                    1,
                    12,
                    0,
                    2,
                    4,
                    0,
                    0,
                    0,
                    0,
                    255,
                    255
                };
                rampRateValue *= 1.8;
                rampRateValue *= 1000.0;
                theData[7] = (byte)unchecked((checked((long)Math.Round(rampRateValue)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(rampRateValue)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(rampRateValue)) >> 8) % 256);
                theData[10] = (byte)Math.Round(rampRateValue % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = CRC16A(ref Buffer2);
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }
        /*
        protected bool SetRampRatedouble(Controller currUSBObject, int intDevAddr, string RampRateValue)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    if (float.TryParse(RampRateValue, out var result))
                    {
                        byte[] bytes = BitConverter.GetBytes(result);
                        Array.Reverse(bytes);
                        b = bytes[0];
                        b2 = bytes[1];
                        b3 = bytes[2];
                        b4 = bytes[3];
                    }
                    theData = new byte[13]
                    {
                        (byte)intDevAddr,
                        16,
                        10,
                        112,
                        0,
                        2,
                        4,
                        b,
                        b2,
                        b3,
                        b4,
                        255,
                        255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                (byte)intDevAddr,
                16,
                1,
                12,
                0,
                1,
                2,
                0,
                0,
                0,
                0,
                255,
                255
                };
                double num2 = double.Parse(RampRateValue);
                num2 *= 1.8;
                num2 *= 1000.0;
                theData[7] = (byte)unchecked((checked((long)Math.Round(num2)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(num2)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(num2)) >> 8) % 256);
                theData[10] = (byte)Math.Round(num2 % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = CRC16A(ref Buffer2);
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }
        */
        /*
        protected bool SetRampRate32(Controller currUSBObject, int intDevAddr, string RampRateValue)
        {
            checked
            {
                byte[] theData = new byte[13]
                {
                (byte)intDevAddr,
                16,
                1,
                12,
                0,
                2,
                4,
                0,
                0,
                0,
                0,
                255,
                255
                };
                double num = double.Parse(RampRateValue);
                num *= 1000.0;
                theData[7] = (byte)unchecked((checked((long)Math.Round(num)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(num)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(num)) >> 8) % 256);
                theData[10] = (byte)Math.Round(num % 256.0);
                byte[] destinationArray = new byte[13];
                Array.Copy(theData, destinationArray, 13);
                int CRCByte = default(int);
                CRClookup(num, ref CRCByte);
                theData[12] = (byte)unchecked(CRCByte % 256);
                theData[11] = (byte)unchecked((CRCByte >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }
        */
        /*
        protected void CRClookup(double thData, ref int CRCByte)
        {
            if (thData == 1000.0)
            {
                CRCByte = 65236;
            }
            else if (thData == 2000.0)
            {
                CRCByte = 64966;
            }
            else if (thData == 3000.0)
            {
                CRCByte = 63784;
            }
            else if (thData == 4000.0)
            {
                CRCByte = 64482;
            }
            else if (thData == 5000.0)
            {
                CRCByte = 62268;
            }
            else if (thData == 6000.0)
            {
                CRCByte = 61566;
            }
            else if (thData == 7000.0)
            {
                CRCByte = 62816;
            }
            else if (thData == 8000.0)
            {
                CRCByte = 63402;
            }
            else if (thData == 9000.0)
            {
                CRCByte = 59204;
            }
            else if (thData == 10000.0)
            {
                CRCByte = 58454;
            }
            else if (thData == 11000.0)
            {
                CRCByte = 57480;
            }
            else if (thData == 12000.0)
            {
                CRCByte = 57922;
            }
            else if (thData == 13000.0)
            {
                CRCByte = 60060;
            }
            else if (thData == 14000.0)
            {
                CRCByte = 59518;
            }
            else if (thData == 15000.0)
            {
                CRCByte = 60768;
            }
            else if (thData == 16000.0)
            {
                CRCByte = 61354;
            }
            else if (thData == 17000.0)
            {
                CRCByte = 53028;
            }
            else if (thData == 18000.0)
            {
                CRCByte = 52278;
            }
            else if (thData == 19000.0)
            {
                CRCByte = 51416;
            }
            else if (thData == 20000.0)
            {
                CRCByte = 51730;
            }
        }
        */
        protected float SetCommstoTempUnitsC(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData = ((!EZ_Zone) ? new byte[8]
                {
                (byte)intDevAddr,
                6,
                0,
                18,
                0,
                1,
                232,
                15
                } : new byte[13]
                {
                (byte)intDevAddr,
                16,
                11,
                154,
                0,
                2,
                4,
                0,
                0,
                0,
                15,
                73,
                136
                });
                currUSBObject.SENDSTRINGusb(ref theData);
                double num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData[11] << 24) + (theData[12] << 16) + (theData[13] << 8) + theData[14]) / 1000.0) : 25.0);
                return (float)num;
            }
        }

        protected float SetCommstoTempUnitsF(Controller currUSBObject, int intDevAddr)
        {
            checked
            {
                byte[] theData = ((!EZ_Zone) ? new byte[8]
                {
                (byte)intDevAddr,
                6,
                0,
                18,
                0,
                0,
                41,
                207
                } : new byte[13]
                {
                (byte)intDevAddr,
                16,
                11,
                154,
                0,
                2,
                4,
                0,
                0,
                0,
                30,
                137,
                132
                });
                currUSBObject.SENDSTRINGusb(ref theData);
                double num = ((currUSBObject.lngTotalBytesRead >= 14) ? ((double)((theData[11] << 24) + (theData[12] << 16) + (theData[13] << 8) + theData[14]) / 1000.0) : 25.0);
                return (float)num;
            }
        }

        protected bool SetIntegralHeat(Controller currUSBObject, int intDevAddr, string SetPtValue, bool bUnitsF)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    if (float.TryParse(SetPtValue, out var result))
                    {
                        byte[] bytes = BitConverter.GetBytes(result);
                        Array.Reverse(bytes);
                        b = bytes[0];
                        b2 = bytes[1];
                        b3 = bytes[2];
                        b4 = bytes[3];
                    }
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    9,
                    70,
                    0,
                    2,
                    4,
                    b,
                    b2,
                    b3,
                    b4,
                    255,
                    255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                (byte)intDevAddr,
                16,
                0,
                224,
                0,
                2,
                4,
                0,
                0,
                0,
                0,
                255,
                255
                };
                double num2 = double.Parse(SetPtValue);
                num2 = (int)Math.Round(num2 * 1000.0);
                theData[7] = (byte)unchecked((checked((long)Math.Round(num2)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(num2)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(num2)) >> 8) % 256);
                theData[10] = (byte)Math.Round(num2 % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = default(int);
                if (!EZ_Zone)
                {
                    num3 = CRC16A(ref Buffer2);
                }
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetDerivativeHeat(Controller currUSBObject, int intDevAddr, string SetPtValue, bool bUnitsF)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    if (float.TryParse(SetPtValue, out var result))
                    {
                        byte[] bytes = BitConverter.GetBytes(result);
                        Array.Reverse(bytes);
                        b = bytes[0];
                        b2 = bytes[1];
                        b3 = bytes[2];
                        b4 = bytes[3];
                    }
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    9,
                    72,
                    0,
                    2,
                    4,
                    b,
                    b2,
                    b3,
                    b4,
                    255,
                    255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                (byte)intDevAddr,
                16,
                0,
                228,
                0,
                2,
                4,
                0,
                0,
                0,
                0,
                255,
                255
                };
                double num2 = double.Parse(SetPtValue);
                num2 = (int)Math.Round(num2 * 1000.0);
                theData[7] = (byte)unchecked((checked((long)Math.Round(num2)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(num2)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(num2)) >> 8) % 256);
                theData[10] = (byte)Math.Round(num2 % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = default(int);
                if (!EZ_Zone)
                {
                    num3 = CRC16A(ref Buffer2);
                }
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }

        protected bool SetProportionBand(Controller currUSBObject, int intDevAddr, string SetPtValue, bool bUnitsF)
        {
            checked
            {
                byte[] theData;
                if (EZ_Zone)
                {
                    byte b = default(byte);
                    byte b2 = default(byte);
                    byte b3 = default(byte);
                    byte b4 = default(byte);
                    if (float.TryParse(SetPtValue, out var result))
                    {
                        byte[] bytes = BitConverter.GetBytes(result);
                        Array.Reverse(bytes);
                        b = bytes[0];
                        b2 = bytes[1];
                        b3 = bytes[2];
                        b4 = bytes[3];
                    }
                    theData = new byte[13]
                    {
                    (byte)intDevAddr,
                    16,
                    9,
                    66,
                    0,
                    2,
                    4,
                    b,
                    b2,
                    b3,
                    b4,
                    255,
                    255
                    };
                    byte[] Buffer = new byte[12];
                    Array.Copy(theData, Buffer, 11);
                    int num = CRC16A(ref Buffer);
                    theData[11] = (byte)unchecked(num % 256);
                    theData[12] = (byte)unchecked((num >> 8) % 256);
                    currUSBObject.SENDSTRINGusb(ref theData);
                    return true;
                }
                theData = new byte[13]
                {
                (byte)intDevAddr,
                16,
                0,
                216,
                0,
                2,
                4,
                0,
                0,
                0,
                0,
                255,
                255
                };
                double num2 = double.Parse(SetPtValue);
                num2 *= 1.8;
                num2 *= 1000.0;
                theData[7] = (byte)unchecked((checked((long)Math.Round(num2)) >> 24) % 256);
                theData[8] = (byte)unchecked((checked((long)Math.Round(num2)) >> 16) % 256);
                theData[9] = (byte)unchecked((checked((long)Math.Round(num2)) >> 8) % 256);
                theData[10] = (byte)Math.Round(num2 % 256.0);
                byte[] Buffer2 = new byte[12];
                Array.Copy(theData, Buffer2, 11);
                int num3 = default(int);
                if (!EZ_Zone)
                {
                    num3 = CRC16A(ref Buffer2);
                }
                theData[11] = (byte)unchecked(num3 % 256);
                theData[12] = (byte)unchecked((num3 >> 8) % 256);
                currUSBObject.SENDSTRINGusb(ref theData);
                return true;
            }
        }
    }
}