using examenProjectSem1_testing.Buffers;
using System;
using System.IO.Ports;
using System.Text;

StringBuilder buffer = new StringBuilder();

SerialPort serialPort = new SerialPort("COM3");

serialPort.BaudRate = 115200;
serialPort.Parity = Parity.None;
serialPort.StopBits = StopBits.One;
serialPort.DataBits = 8;
serialPort.Handshake = Handshake.None;
//serialPort.ReceivedBytesThreshold = 1;

serialPort.Open();

Console.WriteLine("Press 'C' to close the serial port.");
Console.WriteLine("Press 'M' to print options.");
Console.WriteLine();

while (true)
{
    switch (char.ToLower(Console.ReadKey().KeyChar))
    {
        case 'c':
            serialPort.Close();
            break;

        case 'o':
            serialPort.Open();
            break;

        case 'm':
            Console.WriteLine("Press 'C' to close the serial port.");
            Console.WriteLine("Press 'O' to reopen the serial port.");
            Console.WriteLine("Press 'M' to print options.");
            Console.WriteLine("Press 'A' to start reading data.");
            Console.WriteLine("Press 'B' to stop reading data.");
            Console.WriteLine("Press 'S' to send test string.");
            break;

        case 'a':
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            break;
        case 'b':
            serialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
            break;

        case 's':
            /*string randomString = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", new Random().Next(5, 11)).Select(s => s[new Random().Next(s.Length)]).Concat(new[] { '\0' }).ToArray());
            Console.WriteLine($"Sending: {randomString}");
            serialPort.Write(randomString);*/
            /*char[] ff = { 'H', 'e', 'l', 'l', 'o', '#' };
            serialPort.Write(ff, 0, 6);*/
            serialPort.Write("miauw\0");
            break;

        case 'h':
            serialPort.Write("LED TOGGLE\0");
            break;
        case 'j':
            serialPort.Write("LED ON\0");
            break;
        case 'k':
            serialPort.Write("LED OFF\0");
            break;
    }
}



void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
{
    SerialPort sp = (SerialPort)sender;
    string indata = sp.ReadExisting();
    buffer.Append(indata);

    if (buffer.ToString().EndsWith('\0'))
    {
        string message = buffer.ToString().Trim();
        buffer.Clear();
        Console.WriteLine($"Data Received: |{message}|");

        //message = message.Split(' ').Last();
        //Console.WriteLine("The character read is: " + (char)Convert.ToInt32(message));
    }

    /*if (buffer.ToString().EndsWith('\n'))
    {
        string message = buffer.ToString().Trim();
        buffer.Clear();
        Console.WriteLine($"Data Received: {message}");
    }*/
}

/*
RingBuffer<int> buffer = new RingBuffer<int>(10);
buffer.Add(1);
buffer.Add(2);

Console.WriteLine(buffer.Read());
Console.WriteLine(buffer.Read());
*/