Imports Microsoft.VisualBasic
Imports System.Text
Public Class CmdInvoke
    Dim ErrOutput As String
    Dim Output As String
    Private Shared process As Process = Nothing
    Private Shared processLock As New Object()
    Dim exec_flag As Boolean = False
    Public Shared Event OutputReceived As EventHandler(Of String)
    Public Shared Event ErrorReceived As EventHandler(Of String)
    Public Shared Event ProcessExited As EventHandler
    Public Shared Sub Execute(Optional command As String = Nothing, Optional cmdArguments As String = Nothing)
        SyncLock processLock
            If NeedRestartProcess(cmdArguments) Then
                StartProcess(cmdArguments)
            End If
            SendCommand(command)
        End SyncLock
    End Sub

    Private Shared Function NeedRestartProcess(args As String) As Boolean
        Return process Is Nothing OrElse process.HasExited
    End Function

    Private Shared Sub StartProcess(arguments As String)
        CleanupProcess()

        process = New Process With {
            .StartInfo = New ProcessStartInfo("cmd.exe", arguments) With {
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .RedirectStandardInput = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .StandardOutputEncoding = Encoding.UTF8,
                .StandardErrorEncoding = Encoding.UTF8
            },
            .EnableRaisingEvents = True
        }

        AddHandler process.OutputDataReceived, AddressOf HandleOutput
        AddHandler process.ErrorDataReceived, AddressOf HandleError
        AddHandler process.Exited, AddressOf HandleProcessExited
        process.EnableRaisingEvents = True ' 必须设置才能触发Exited事件
        process.Start()
        process.BeginOutputReadLine()
        process.BeginErrorReadLine()

    End Sub

    Private Shared Sub HandleProcessExited(sender As Object, e As EventArgs)
        RaiseEvent ProcessExited(Nothing, EventArgs.Empty)
        CleanupProcess()
    End Sub

    Private Shared Sub HandleOutput(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            RaiseEvent OutputReceived(Nothing, e.Data)
            Debug.WriteLine($"[CONSOLE] {e.Data}")
        End If
    End Sub

    Private Shared Sub HandleError(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            RaiseEvent ErrorReceived(Nothing, e.Data)
            Debug.WriteLine($"[ERROR] {e.Data}")

        End If
    End Sub

    Private Shared Sub SendCommand(command As String)
        Try
            If process?.HasExited = False Then
                process.StandardInput.WriteLine(command)
                process.StandardInput.Flush()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[SEND ERROR] {ex.Message}")
        End Try
    End Sub

    Public Shared Sub KillProcess()
        SyncLock processLock
            CleanupProcess()
        End SyncLock
    End Sub

    Private Shared Sub CleanupProcess()
        If process IsNot Nothing Then
            RemoveHandler process.OutputDataReceived, AddressOf HandleOutput
            RemoveHandler process.ErrorDataReceived, AddressOf HandleError
            If Not process.HasExited Then process.Kill()
            process.Dispose()
            process = Nothing
        End If
    End Sub

End Class
