using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Console;
using Windows.Win32.System.Threading;
using Markdig.Helpers;

namespace testcmd;
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32.SafeHandles;

public class RingBuffer
{
    public DispatcherQueue DispatcherQueue;
    public (short, short) ViewportSize;
    public (int,int) BufferSize;
    private byte[,] _buffer;
    private int _ringLineNum;
    private int _viewPortTop;
    private int _furthestWrite;
    public (short, short) CursorPos;
    public SemaphoreSlim BufferLock= new SemaphoreSlim(1,1);
    public TextBox TextBox;
    
    public RingBuffer((int,int) bufferSize,(short,short)viewportSize)
    {
        ViewportSize = viewportSize;
        BufferSize = bufferSize;
        _buffer =  new byte[bufferSize.Item1, bufferSize.Item2];
    }
    public string PrintString()
    {
        StringBuilder sb = new StringBuilder();
        int line = _viewPortTop-1;
        while(line != _furthestWrite)
        {
            line++;
            line%= BufferSize.Item1;
            for(int i=0;i<ViewportSize.Item2;i++)
            {
                sb.Append((char)_buffer[line,i]);
            }

            sb.AppendLine();
        }
        return sb.ToString();
    }
    public void Print()
    {
        
        Console.Clear();
        int line = _viewPortTop-1;
        while(line != _furthestWrite)
        {
            line++;
            line%= BufferSize.Item1;
            for(int i=0;i<ViewportSize.Item2;i++)
            {
                Console.Write((char)_buffer[line,i]);
            }
            Console.WriteLine();
        }
    }
    public void MoveCursorLeft(short step=1)
    {
        CursorPos.Item1-=step;
        if(CursorPos.Item1 < 0)
        {
            CursorPos.Item1=0;
        }
    }
    public void EraseToEndOfLine()
    {
        for(int i = CursorPos.Item2;i<ViewportSize.Item2 ; i++)
        {
            _buffer[ViewportYToAbsolute(CursorPos.Item1),i] = 0;
        }
    }
    public void EraseLine()
    {
        
        for(int i =0;i<ViewportSize.Item2 ; i++)
        {
            _buffer[ViewportYToAbsolute(CursorPos.Item1),i] = 0;
        }
    }
    public void EraseToCursor()
    {
        for(int i =0;i<=CursorPos.Item2 ; i++)
        {
            _buffer[ViewportYToAbsolute(CursorPos.Item1),i] = 0;
        }
    }
    public void MoveCursorRight(short step=1)
    {
        if(CursorPos.Item1 +step< ViewportSize.Item1)
        {
            CursorPos.Item1+= step;
        }
    }
    public void ScrollUp()
    {
        _viewPortTop--;
        if(_viewPortTop < 0)
        {
            _viewPortTop = 0;
        }
    }
    public int ViewportYToAbsolute(int y)
    {
        return  (_ringLineNum+_viewPortTop+y)%BufferSize.Item1;
    } 
    public void SetCursorY(short y)
    {
        CursorPos.Item2 = (short)Math.Clamp((int)y,0,(int)ViewportSize.Item2);
    }
    public void SetCursorX(short x)
    {
        CursorPos.Item1 = (short)Math.Clamp((int)x,0,(int)ViewportSize.Item1);
    }
    public void ScrollDown()
    {
        _viewPortTop++;
        _viewPortTop %=  BufferSize.Item1;
    }
    public void MoveCursorDown(short step=1,bool doScroll=false)
    {
        if(CursorPos.Item2+step < ViewportSize.Item2)
        {
            CursorPos.Item2+=step;
        }else if(doScroll){
            ScrollDown();
        }
    }
    public void MoveCursorUp(short step=1)
    {
        CursorPos.Item2-=step;
        if(CursorPos.Item2 <0)
        {
            CursorPos.Item2 = 0;
        }
    }
    public void Write(byte value)
    {
        int line = _ringLineNum + _viewPortTop + CursorPos.Item2;
        _furthestWrite = Math.Max(line+1,_furthestWrite);
        _buffer[(_ringLineNum+_viewPortTop+CursorPos.Item2)%BufferSize.Item2, CursorPos.Item1%ViewportSize.Item1] = value;
        if(!DispatcherQueue.TryEnqueue(() =>
        {

            TextBox.Text = PrintString();
        }))
        {

        }
    }
    
}
public class PseudoConsole: IDisposable
{
    private SafeFileHandle _inputReadSide;
    private SafeFileHandle _outputWriteSide;
    private (int, int)? CursorMemory;
    public FileStream InputStream;
    public FileStream FunStream;
    public FileStream OutputStream;
    private ClosePseudoConsoleSafeHandle _pcHandle;
    private bool _disposed = false;
    public (short, short) Size;
    private SemaphoreSlim _semaphore = new(1,1);
    private Queue<byte> _stdinPipe= new();
    public RingBuffer Buffer;
    public List<byte> DebugList = [];
    public PseudoConsole((int,int) bufferSize,(short,short) size, DispatcherQueue dispatcherQueue)
    {
        
        Size = size;
        SafeFileHandle inputReadSide = new SafeFileHandle(IntPtr.Zero, true);
        SafeFileHandle inputWriteSide = new SafeFileHandle(IntPtr.Zero, true);
        SafeFileHandle outputReadSide = new SafeFileHandle(IntPtr.Zero, true);
        SafeFileHandle outputWriteSide = new SafeFileHandle(IntPtr.Zero, true);
         
        if(!PInvoke.CreatePipe(out inputReadSide,out inputWriteSide,null,0))
        {
            throw new Exception("Failed to create pipe");
        }
        if(!PInvoke.CreatePipe(out outputReadSide,out outputWriteSide,null,0))
        {
            inputReadSide.Dispose();
            inputWriteSide.Dispose();
            throw new Exception("Failed to create pipe");
        }
        
        COORD sizeCoords;
        sizeCoords.X = size.Item1;
        sizeCoords.Y = size.Item2;
        int result =PInvoke.CreatePseudoConsole(sizeCoords, inputReadSide, outputWriteSide, 0, out _pcHandle);
        if(result!=0){
        
            inputReadSide.Dispose();
            inputWriteSide.Dispose();
            outputReadSide.Dispose();
            outputWriteSide.Dispose();
            throw new Exception("failed to create pseudo console");
        }
        FunStream = new FileStream(outputWriteSide, FileAccess.Write);
        InputStream = new FileStream(inputWriteSide, FileAccess.Write);
        OutputStream= new FileStream(outputReadSide, FileAccess.Read);
        _outputWriteSide = outputWriteSide;
        _inputReadSide = inputReadSide;
        
        
        
        
        STARTUPINFOEXW si = new STARTUPINFOEXW();
        si.StartupInfo.cb =(uint)Marshal.SizeOf(typeof(STARTUPINFOEXW));

        UIntPtr bytesRequired;
        unsafe
        {

            PInvoke.InitializeProcThreadAttributeList(LPPROC_THREAD_ATTRIBUTE_LIST.Null, 1, 0, &bytesRequired);
            si.lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)PInvoke.HeapAlloc(PInvoke.GetProcessHeap(), 0, bytesRequired);
            if(!PInvoke.InitializeProcThreadAttributeList(si.lpAttributeList,1,0,&bytesRequired))
            {
                PInvoke.HeapFree(PInvoke.GetProcessHeap(),0,si.lpAttributeList); 
                Console.WriteLine("Failed to initialize process heap");
            }
            if(!PInvoke.UpdateProcThreadAttribute(si.lpAttributeList,
                0,
                131094,
                (void*)_pcHandle.DangerousGetHandle(),
                (UIntPtr)sizeof(nint),
                null,
                (nuint?)null)){
                
                PInvoke.HeapFree(PInvoke.GetProcessHeap(),0,si.lpAttributeList); 
                Console.WriteLine("Failed to update process heap");
            }

            PROCESS_INFORMATION pi;
            string cmd = "cmd.exe";
            IntPtr cmdAlloc = Marshal.StringToHGlobalUni(cmd);
            PWSTR cmdStr = new PWSTR(cmdAlloc);
            if(!PInvoke.CreateProcess(null,
                cmdStr,
                null,
                null,
                false,
                PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
                null,
                null,
                &si.StartupInfo,
                &pi)){
                Console.WriteLine("Failed to create process");
            }

            Marshal.FreeHGlobal(cmdAlloc);
            PInvoke.HeapFree(PInvoke.GetProcessHeap(),0,si.lpAttributeList);
        }


        Buffer = new RingBuffer(bufferSize, size);
        Buffer.DispatcherQueue = dispatcherQueue;
    }
    private async Task<(byte,bool)> ReadNext(CancellationToken ct)
    {
        await _semaphore.WaitAsync();
        try
        {
            if(_stdinPipe.TryDequeue(out var ch))
            {
            
                return (ch,false);
            }
        }finally{
            _semaphore.Release();
        }
        byte[] buffer = new byte[1];
        await OutputStream.ReadExactlyAsync(buffer,ct);
        DebugList.Add(buffer[0]);
        return (buffer[0],true);
    }
    private async Task<(int,byte)> ParseInt(CancellationToken ct,char? firstChar = null)
    {
        List<char> chars = [];
        if(firstChar.HasValue)
        {
            chars.Add(firstChar.Value); 
        }
        var nextByte = await UpdateBuffer(ct);
        while(((char)nextByte).IsDigit())
        {
            chars.Add((char)nextByte);
            nextByte = await UpdateBuffer(ct);
        }
        return (Int32.Parse(String.Join("", chars)),nextByte);
    }
    public async Task BufferLoop(CancellationToken ct)
    {
        while(true) 
        {
            await UpdateBuffer(ct,true); 
            // Buffer.Print(); 
        }
    }
    private async Task ParseCursorPositioning(CancellationToken ct)
    {
        var nextByte = (char)await UpdateBuffer(ct);
        if(nextByte.IsDigit())
        {
            List<char> chars = [nextByte];
            nextByte = (char)await UpdateBuffer(ct);
            while(nextByte.IsDigit())
            {
                chars.Add(nextByte);
                nextByte = (char)await UpdateBuffer(ct);
            }
            short num = (short)Int32.Parse(String.Join("", chars));
            switch(nextByte)
            {
                case ';':
                    
                    List<char>  num2arr= [];
                    nextByte = (char)await UpdateBuffer(ct);
                    while(nextByte.IsDigit())
                    {
                        num2arr.Add(nextByte);
                        nextByte = (char)await UpdateBuffer(ct);
                    }
                    short num2 = (short)Int32.Parse(String.Join("",num2arr));
                    Buffer.SetCursorX((short)(num2-1)); 
                    Buffer.SetCursorY((short)(num-1)); 
                    break;
                case 'A':
                    Buffer.MoveCursorUp(num);
                    break;
                case 'B':
                    Buffer.MoveCursorDown(num);
                    break;
                case 'C':
                    Buffer.MoveCursorRight(num);
                    break;
                case 'D':
                    Buffer.MoveCursorLeft(num);
                    break;
                case 'E':
                    Buffer.MoveCursorDown((short)(num+1));
                    Buffer.SetCursorX(0);
                    break;
                case 'F':
                    Buffer.MoveCursorUp((short)(num+1));
                    Buffer.SetCursorX(0);
                    break;
                case 'G':
                    Buffer.SetCursorX(num);
                    break;
                case 'n':
                    if(num==6)
                    {
                        await _semaphore.WaitAsync();
                        try
                        {
                            
                            _stdinPipe.Enqueue(0x1b);
                            _stdinPipe.Enqueue((byte)'[');
                            string x = Buffer.CursorPos.Item1+"";
                            string y = Buffer.CursorPos.Item2+"";
                            foreach(char c in x)
                            {
                                _stdinPipe.Enqueue((byte)c);
                            } 
                            _stdinPipe.Enqueue((byte)';');
                            foreach(char c in y)
                            {
                                _stdinPipe.Enqueue((byte)c);
                            } 
                            _stdinPipe.Enqueue((byte)'R');
                        }finally{
                            _semaphore.Release();
                        }
                    }
                    break;
                case 'K':
                    switch(num)
                    {
                        case (0):
                            Buffer.EraseToEndOfLine();
                            break;
                        case 1:
                            Buffer.EraseToCursor();
                            break;
                        case 2:
                            Buffer.EraseLine();
                            break;
                    } 
                    break;
                
            }
        }else{
            switch(nextByte)
            {
                case 'M':   
                    Buffer.MoveCursorUp();
                    break;
                case '?':
                    while (((char)await UpdateBuffer(ct)).IsDigit()) ;
                    break;
                case 'K':
                    Buffer.EraseToEndOfLine();
                    break; 
                // case '7':
                //     CursorMemory = Buffer.CursorPos;
                //     break;
                // case '8':
                //     if(CursorMemory!= null)
                //     {
                //         
                //     }
                //     break;
            }
        }
         
    }
    private async Task ParseCommandSequence(CancellationToken ct)
    {
        var p = await UpdateBuffer(ct);
        var k  = await ParseInt(ct, (char)p);
        p = k.Item2;
        
        int i = 0;
        while (p != (byte)'\a' && p!= 0x1b)
        {
            i++;
            p = await UpdateBuffer(ct);
        }
        if(p== 0x1b)
        {
            await UpdateBuffer(ct);
        }
    }
    private async Task ParseEscapeSequence(CancellationToken ct)
    {
        var nextByte = await UpdateBuffer(ct);
        switch(nextByte)
        {
            case (byte)'[':
                await ParseCursorPositioning(ct);
                break;
            case (byte) ']':
                await ParseCommandSequence(ct);
                break;
                
        }
    }
    public async Task<byte> UpdateBuffer(CancellationToken ct, bool doWrite=false)
    {
        bool isOut;
        byte finalByte;
        do
        {
            var (nextByte, new_out) = await ReadNext(ct);
            // Console.Write(nextByte+" ");
            
            finalByte = nextByte;
            isOut = new_out;
            if(!doWrite && isOut)
            {
                continue;
            }
            
            switch (nextByte)
            {
                case 0x1b:
                    if (isOut)
                    {
                        await ParseEscapeSequence(ct);
                    }

                    break;
                case (byte)'\n':
                    Buffer.MoveCursorDown(doScroll:true);
                    Buffer.SetCursorX(0);
                    break;
                case (byte)'\r':
                    Buffer.SetCursorX(0);
                    break;
                case (byte)'\b':
                    Buffer.MoveCursorLeft();
                    break;
                case (byte)'\t':
                    int curX = Buffer.CursorPos.Item1;
                    int jump = 8 - curX % 8;
                    jump = Math.Max(1, jump);
                    Buffer.MoveCursorRight((short)jump);
                    break;
                default:
                    // Console.WriteLine(nextByte);
                    // Console.WriteLine((char)nextByte);
                    // Console.WriteLine();
                    Buffer.Write(nextByte);
                    Buffer.MoveCursorRight();
                    break;
            }
        } while (!isOut);
        return finalByte;
    }
    public async Task SendInput(byte[] data,CancellationToken ct)
    {
        // await _semaphore.WaitAsync();   
        // try{
        //     foreach(byte b in data)
        //     {
        //         _stdinPipe.Enqueue(b);
        //     }
        // } finally{
        //     _semaphore.Release(); 
        // }
        //
        await InputStream.WriteAsync(data, ct);
    }
    public async Task SendCommand(string command, CancellationToken ct)
    {
        
        command += "\r\n";
        var data = Encoding.ASCII.GetBytes(command);
        await SendInput(data, ct);
    }

    public void Dispose()
    {
        if(_disposed)
        {
            return;
        }
        _inputReadSide.Dispose();
        InputStream.Dispose();
        OutputStream.Dispose();
        FunStream.Dispose();
        _pcHandle.Dispose(); 
        
    }
}