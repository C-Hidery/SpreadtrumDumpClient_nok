Imports System.IO
Imports System.Text
Imports System.Windows.Threading
Imports Microsoft.VisualBasic




Public Class LogRedirector
    Implements IDisposable

    Private ReadOnly _textBox As TextBox
    Private _originalConsoleOut As TextWriter
    Private _isDisposed As Boolean = False
    Private _listener As TraceListener

    ''' <summary>
    ''' 创建日志重定向器
    ''' </summary>
    ''' <param name="textBox">用于显示日志的TextBox控件</param>
    Public Sub New(textBox As TextBox)
        ' VB.NET 风格的空值检查
        If textBox Is Nothing Then
            Throw New ArgumentNullException(NameOf(textBox))
        End If

        _textBox = textBox

        ' 保存原始控制台输出
        _originalConsoleOut = Console.Out

        ' 创建自定义TraceListener
        _listener = New TextBoxTraceListener(AddressOf AppendText)

        ' 添加自定义监听器到Debug
        Trace.Listeners.Add(_listener)

        ' 重定向控制台输出
        Console.SetOut(New TextBoxWriter(AddressOf AppendText))
    End Sub

    ''' <summary>
    ''' 将文本安全地追加到TextBox
    ''' </summary>
    Private Sub AppendText(text As String)
        ' 确保在UI线程上操作
        If _textBox.Dispatcher.CheckAccess() Then
            AppendTextInternal(text)
        Else
            _textBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                               New Action(Of String)(AddressOf AppendTextInternal),
                                               text)
        End If
    End Sub

    ''' <summary>
    ''' 实际追加文本到TextBox
    ''' </summary>
    Private Sub AppendTextInternal(text As String)
        ' 添加文本并自动滚动到底部
        _textBox.AppendText(text)
        _textBox.CaretIndex = _textBox.Text.Length
        _textBox.ScrollToEnd()
    End Sub

    ''' <summary>
    ''' 清理资源
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        If _isDisposed Then Return

        ' 恢复原始控制台输出
        Console.SetOut(_originalConsoleOut)

        ' 清理TraceListeners
        Trace.Listeners.Remove(_listener)

        ' 清理自定义监听器
        If TypeOf _listener Is IDisposable Then
            DirectCast(_listener, IDisposable).Dispose()
        End If

        _isDisposed = True
    End Sub

    ''' <summary>
    ''' 自定义TraceListener
    ''' </summary>
    Private Class TextBoxTraceListener
        Inherits TraceListener

        Private ReadOnly _appendAction As Action(Of String)

        Public Sub New(appendAction As Action(Of String))
            _appendAction = appendAction
        End Sub

        Public Overrides Sub Write(message As String)
            _appendAction(message)
        End Sub

        Public Overrides Sub WriteLine(message As String)
            _appendAction(message & Environment.NewLine)
        End Sub
    End Class

    ''' <summary>
    ''' 自定义TextWriter
    ''' </summary>
    Private Class TextBoxWriter
        Inherits TextWriter

        Private ReadOnly _appendAction As Action(Of String)

        Public Sub New(appendAction As Action(Of String))
            _appendAction = appendAction
        End Sub

        Public Overrides Sub Write(value As Char)
            _appendAction(value.ToString())
        End Sub

        Public Overrides Sub Write(value As String)
            _appendAction(value)
        End Sub

        Public Overrides Sub WriteLine()
            _appendAction(Environment.NewLine)
        End Sub

        Public Overrides Sub WriteLine(value As String)
            _appendAction(value & Environment.NewLine)
        End Sub

        Public Overrides ReadOnly Property Encoding As Encoding
            Get
                Return Encoding.UTF8
            End Get
        End Property
    End Class
End Class