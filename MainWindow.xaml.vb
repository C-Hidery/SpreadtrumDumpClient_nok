Imports System.Collections.Specialized
Imports System.IO
Imports System.IO.Ports
Imports System.Runtime.InteropServices.JavaScript.JSType
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Thread
Imports System.Windows.Threading
Imports Microsoft.Win32
Imports SPRDClientCore.Models
'Imports SPRDClientCore.Program
Imports SPRDClientCore.Protocol
Imports SPRDClientCore.Protocol.Encoders
Imports SPRDClientCore.Utils
Imports System.Collections.Generic
Imports System.Xml.Linq

'Imports SpreadtrumDumpClient.SprdFlashBinary


Public Class MainWindow
    ' Private WithEvents diag As New SprdU2SDiag()
    ' Private WithEvents spdinit As New SprdDeviceInit()
    '  Private WithEvents spdflash As New SprdPartitionManager()
    Private FDL1_Loaded As Boolean = False
    Private FDL2_Executed As Boolean = False
    ' Private WithEvents spd As New SprdFlashBinary() 'fail(
    'Private output As New OutputRedirector(txtOutput)
    Public device_mode As String
    Public sprdmode As String
    Private portName_dl As String = "SPRD U2S Diag"
    Public partitionName As String
    Private Date_1 = $"[{DateTime.Now:HH:mm:ss}]"
    'Private round As Integer = 0
    ' Private FDL_sent As Boolean
    '  Private OP_flag As Boolean
    Private utils As SPRDClientCore.Utils.SprdFlashUtils
    Public partitions As (List(Of Partition), GetPartitionsMethod)
    Public isSPRD4NoFDL As Boolean = False

    Private Sub Form1_Loaded(sender As Object, e As RoutedEventArgs)
start_1:


        ' output.Initialize(txtOutput)

    End Sub
    Public Sub New()

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
        'Thread.Sleep(500)

    End Sub

    Private Async Sub connect_1_Click(sender As Object, e As RoutedEventArgs) Handles connect_1.Click

        Dim r1 As New LogRedirector(txtOutput)
        'Dim r2 As New ProgressMonitor(progressBar_1)
        Dim timeout1 As Integer = Int(wait_con.Text) * 1000
        Dim timeout2 = wait_con.Text
        Dim port As String
        Write_con("Waiting for connection")
        connect_1.IsEnabled = False
        Dim sprd4_1 = sprd4.IsOn
        Dim stage As (Stages, Stages)
        Dim result
        Await Task.Run(Sub()

                           Try
                               AppendToOutput($"{Date_1} Begin to boot...({timeout2}s)")
                               AppendToOutput($"{Date_1} Based on SPRDClientCore 1.0.1.0")
                               port = SprdProtocolHandler.FindComPort(timeout:=timeout1)
                               DEG_LOG($"Find port: {port}", "I")
                               Dim handler As New SprdProtocolHandler(port, New HdlcEncoder())
                               utils = New SprdFlashUtils(handler)
                               AddHandler utils.UpdatePercentage, AddressOf UpdateProgressInvoke
                               If Not sprd4_1 Then
                                   stage = utils.ConnectToDevice()
                               Else
                                   Dispatcher.BeginInvoke(Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                 .Title = "Kicking device to BROM mode",
                                                                                                                                                                                                                 .Content = $"Program kicked device to BROM mode, reconnecting...{Environment.NewLine} 已将设备踢进BROM模式，重连中...",
                                                                                                                                                                                                                 .PrimaryButtonText = "OK",
                                                                                                                                                                                                                 .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                             }
                                                              dialog.ShowAsync()
                                                          End Sub)

                                   SprdFlashUtils.ChangeDiagnosticMode(handler)
                                   stage = utils.ConnectToDevice()


                               End If

                               Thread.Sleep(1000)
                               device_mode = stage.Item2
                               sprdmode = stage.Item1
                               Dim mode = stage.Item2.ToString
                               DEG_LOG($"Device mode: {mode}", "I")
                               If stage.Item1 = Stages.Sprd4 Then
                                   Dispatcher.BeginInvoke(Async Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                 .Title = "No FDL mode Available",
                                                                                                                                                                                                                 .Content = $"Your device is in SPRD4 mode, you may operate partitions without FDL.WARNING:This feature is not supported on some devices, and it is not supported on devices with corrupted system partitions.Do you want to enable this?{Environment.NewLine} 你的设备处于SPRD4模式，可以实现无FDL实现分区操作。警告:某些设备不支持此功能，system分区损坏的设备不支持此功能。你想要启用它吗？",
                                                                                                                                                                                                                 .PrimaryButtonText = "Yes",
                                                                                                                                                                                                                 .SecondaryButtonText = "No",
                                                                                                                                                                                                                 .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                             }
                                                              result = Await dialog.ShowAsync()
                                                              If result = ContentDialogResult.Primary Then
                                                                  isSPRD4NoFDL = True
                                                                  utils.ExecuteDataAndConnect(Stages.Brom)
                                                                  utils.ExecuteDataAndConnect(Stages.Fdl1)
                                                                  Write_con("Successfully booted")
                                                                  Write_mode($"FDL2-Executed: Sprd{sprdmode}")

                                                                  dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Successfully connected and executed!",
                                                                                                                                                    .Content = $"Successfully execute device, please operate!{Environment.NewLine} 引导设备成功，请操作！",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                                  dialog.ShowAsync()

                                                                  Set_Title1()
                                                                  Enable_buttons()
                                                                  Init_fdl2()
                                                              Else
                                                                  isSPRD4NoFDL = False
                                                              End If
                                                          End Sub)

                               End If
                           Catch ex1 As TimeoutException
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Timeout",
                                                                                                                                                    .Content = $"Can not connect to device: timeout!!! Exiting...{Environment.NewLine} 连接失败:超时，退出中...{Environment.NewLine} {ex1.Message}",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()

                                                      End Sub)

                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           Catch ex2 As ExceptionDefinitions.ResponseTimeoutReachedException
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "No response",
                                                                                                                                                    .Content = $"Can not connect to device: timeout!!! Exiting...{Environment.NewLine} 连接失败:超时，退出中...{Environment.NewLine} {ex2.Message}",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()

                                                      End Sub)

                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           Catch ex3 As System.IO.IOException
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Emm",
                                                                                                                                                    .Content = $"{ex3.Message}{Environment.NewLine} Exiting...  退出中...",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()

                                                      End Sub)

                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End Try
                           If device_mode = Stages.Brom Then
                               Write_con("Successfully connected, please execute FDL1")
                               Write_mode($"BROM: Sprd{sprdmode}")
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Successfully connected",
                                                                                                                                                    .Content = $"Successfully connect to device, please execute FDL1!{Environment.NewLine} 连接设备成功，请发送FDL1！",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()
                                                          fdl_exec.IsEnabled = True
                                                      End Sub)
                           ElseIf device_mode = Stages.Fdl1 Then
                               Write_con("Successfully connected, please execute FDL2")
                               Write_mode($"FDL1: Sprd{sprdmode}")
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Successfully connected",
                                                                                                                                                    .Content = $"Successfully connect to device, please execute FDL2!{Environment.NewLine} 连接设备成功，请发送FDL2！",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()
                                                          fdl_exec.IsEnabled = True
                                                      End Sub)
                           ElseIf device_mode = Stages.Fdl2 Then
Exec:
                               Write_con("Successfully booted")
                               Write_mode($"FDL2-Executed: Sprd{sprdmode}")
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Successfully connected and executed!",
                                                                                                                                                    .Content = $"Successfully execute device, please operate!{Environment.NewLine} 引导设备成功，请操作！",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()
                                                      End Sub)
                               Set_Title1()
                               Enable_buttons()
                               Init_fdl2()
                           Else
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                    .Title = "Failed to connect",
                                                                                                                                                    .Content = $"Can not connect to device: Unknown error. Exiting...{Environment.NewLine} 连接失败:未知错误，退出中...",
                                                                                                                                                    .PrimaryButtonText = "OK",
                                                                                                                                                    .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                }
                                                          dialog.ShowAsync()

                                                      End Sub)

                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End If
                       End Sub)



    End Sub





    Public Sub LoadPartitionsFromXml(filePath As String, ByRef partitions As List(Of Partition))
        ' 确保列表已初始化
        partitions = If(partitions, New List(Of Partition)())

        ' 加载XML文档
        Dim xmlDoc As XDocument = XDocument.Load(filePath)

        ' 查找所有Partition节点
        Dim partitionNodes = xmlDoc.Descendants("Partition")

        For Each partNode In partitionNodes
            ' 提取分区名称 (id属性)
            Dim partitionName = partNode.Attribute("id")?.Value

            ' 提取并转换大小 (size属性)
            Dim sizeText = partNode.Attribute("size")?.Value
            If Not String.IsNullOrWhiteSpace(sizeText) AndAlso
           Not String.IsNullOrWhiteSpace(partitionName) Then

                ' 解析大小并转换为字节 (MB * 1024 * 1024)
                Dim sizeMB As Double
                If Double.TryParse(sizeText, sizeMB) Then
                    Dim partition As New Partition With {
                    .Name = partitionName,
                    .Size = CULng(sizeMB * 1024 * 1024) ' 转换为字节
                }
                    partitions.Add(partition)
                End If
            End If
        Next
    End Sub







    Public perhaps_parts As Object = {
    "splloader",
    "boot",
    "system",
    "recovery",
    "vbmeta",
    "logo",
    "persist",
    "misc",
    "vendor",
    "dtbo",
    "prodnv",
    "sml",
    "sml_bak",
    "uboot",
    "miscdata",
    "l_fixnv1",
    "l_fixnv2",
    "cache",
    "userdata",
    "data",
    "l_modem",
    "oem",
    "modem",
    "firmware",
    "dsp",
    "keystore",
    "rpm",
    "lk",
    "reserve"
}


    ''' <summary>
    ''' Print Contents to DEBUG window and TextBox
    ''' </summary>
    ''' <param name="Text">Contents</param>
    ''' <param name="str">Information Level(I,W,E)</param>
    Public Sub DEG_LOG(Text As String, str As String)



        If str = "I" Then

            Debug.WriteLine($"{Date_1} [INFO] {Text}")
            AppendToOutput($"{Date_1} [INFO] {Text}")
        End If
        If str = "W" Then

            Debug.WriteLine($"{Date_1} [WARN] {Text}")
            AppendToOutput($"{Date_1} [WARN] {Text}")
        End If
        If str = "E" Then

            Debug.WriteLine($"{Date_1} [ERROR] {Text}")
            AppendToOutput($"{Date_1} [ERROR] {Text}")
        End If
    End Sub
    '引导完成，初始化


    '获取分区名




    Private Async Sub fdl_exec_Click(sender As Object, e As RoutedEventArgs)
        Write_con("Executing...")
        Dim file1 = fdl_file_path.Text
        Dim addr = fdl_addr.Text
        Dim cve = exec_addr.IsOn
        Dim cve_path_1 = cve_addr.Text
        Dim fileName As String = Path.GetFileNameWithoutExtension(cve_path_1)
        Dim lastUnderscorePos As Integer = fileName.LastIndexOf("_"c)

        ' 提取下划线后的8个字符
        Dim result As String = ""
        If lastUnderscorePos > 0 AndAlso lastUnderscorePos + 8 < fileName.Length Then
            result = fileName.Substring(lastUnderscorePos + 1, 8)
        End If
        Dim cve_addr_1 = "0x" + result
        Await Task.Run(Sub()
                           Dim fs As FileStream = File.OpenRead(file1)
                           Try

                               If device_mode = Stages.Brom Then

                                   If Not cve Then
                                       utils.SendFile(fs, startAddress:=SprdFlashUtils.StringToSize(addr))
                                       utils.ExecuteDataAndConnect(Stages.Brom)
                                   Else
                                       utils.SendFile(fs, startAddress:=SprdFlashUtils.StringToSize(addr))
                                       Dim ft As FileStream = File.OpenRead(cve_path_1)
                                       utils.SendFile(ft, startAddress:=SprdFlashUtils.StringToSize(cve_addr_1))
                                       utils.ExecuteDataAndConnect(Stages.Brom)
                                   End If

                               ElseIf device_mode = Stages.Fdl1 Then
                                   utils.SendFile(fs, startAddress:=SprdFlashUtils.StringToSize(addr))
                                   utils.ExecuteDataAndConnect(Stages.Fdl1)

                               End If

                               If Not FDL1_Loaded Then
                                   device_mode = Stages.Fdl1
                                   FDL1_Loaded = True
                                   Dispatcher.BeginInvoke(Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                             .Title = "Successfully execute FDL1!",
                                                                                                                                                                                                             .Content = $"Successfully execute FDL1, please execute FDL2!{Environment.NewLine} 引导FDL1成功，请发送FDL2！",
                                                                                                                                                                                                             .PrimaryButtonText = "OK",
                                                                                                                                                                                                             .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                         }
                                                              dialog.ShowAsync()
                                                          End Sub)
                                   Write_con("Please execute FDL2!")
                                   Write_mode($"FDL1: Sprd{sprdmode}")
                               ElseIf Not FDL2_Executed Then
                                   device_mode = Stages.Fdl2
                                   FDL2_Executed = True
                                   Dispatcher.BeginInvoke(Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                             .Title = "Successfully booted!",
                                                                                                                                                                                                             .Content = $"Successfully booted, please operate!{Environment.NewLine} 引导设备成功，请操作！",
                                                                                                                                                                                                             .PrimaryButtonText = "OK",
                                                                                                                                                                                                             .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                         }
                                                              dialog.ShowAsync()
                                                          End Sub)
                                   Write_con("Successfully booted")
                                   Write_mode($"FDL2: Sprd{sprdmode}")
                                   Set_Title1()
                                   Enable_buttons()
                                   Init_fdl2()
                               End If

                           Catch ex1 As ExceptionDefinitions.UnexceptedResponseException
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                         .Title = "Can not execute",
                                                                                                                                                                                                         .Content = $"Response not correct, exiting...{Environment.NewLine} 响应不正确,退出中...{Environment.NewLine}{ex1.Message}",
                                                                                                                                                                                                         .PrimaryButtonText = "OK",
                                                                                                                                                                                                         .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                     }
                                                          dialog.ShowAsync()
                                                      End Sub)
                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           Catch ex2 As Exception
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                         .Title = "Emm...",
                                                                                                                                                                                                         .Content = $"{ex2.Message}{Environment.NewLine}Exiting...  退出中...",
                                                                                                                                                                                                         .PrimaryButtonText = "OK",
                                                                                                                                                                                                         .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                     }
                                                          dialog.ShowAsync()

                                                      End Sub)
                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End Try


                       End Sub)






    End Sub
    Public Async Sub Init_fdl2()
        Dim method As String
        Dim part_list1 As New List(Of Partition)
        Await Task.Run(Sub()
                           partitions = utils.GetPartitionsAndStorageInfo()
                           method = partitions.Item2
                           part_list1 = partitions.Item1
                           For Each i In part_list1
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim chk As New ListBoxItem
                                                          chk.Content = $"{part_list.Items.Count + 1}. {i.Name} {i.Size / 1024 / 1024} MB"
                                                          part_list.Items.Add(chk)
                                                      End Sub)

                           Next
                           If method = GetPartitionsMethod.ConvertExtTable Then
                               DEG_LOG("Get part list through method 1 successfully.", "I")
                           ElseIf method = GetPartitionsMethod.SendReadPartitionPacket Then
                               DEG_LOG("Get part list through method 2 successfully.", "I")
                           ElseIf method = GetPartitionsMethod.TraverseCommonPartitions Then
                               DEG_LOG("Get part list through compatibility method successfully.", "I")
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Emm...",
                                                                                                                                                                                                                                                                       .Content = $"Can not read partition list through common way, we will try compatibility method.{Environment.NewLine} 常规方法获取分区列表失败，我们将尝试兼容性方法。",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()
                                                      End Sub)
                           Else
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Emm...",
                                                                                                                                                                                                                                                                       .Content = $"Can not read partition list through any way, you may operate in 'Manually Operate'{Environment.NewLine} 获取分区列表失败，请在‘手动操作’中操作",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()
                                                      End Sub)
                           End If

                       End Sub)
    End Sub
    Private Sub Set_Title1()

        If ti_c.Dispatcher.CheckAccess() Then
            ' 当前线程是 UI 线程，直接操作
            ti_c.Content = "设备已连接!"
        Else
            ' 跨线程调用，使用 Dispatcher
            ti_c.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub()
                               ti_c.Content = "设备已连接!"
                           End Sub))
        End If
        If ti_e.Dispatcher.CheckAccess() Then
            ' 当前线程是 UI 线程，直接操作
            ti_e.Content = "Device connected!"
        Else
            ' 跨线程调用，使用 Dispatcher
            ti_e.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub()
                               ti_e.Content = "Device connected!"
                           End Sub))
        End If


    End Sub
    Public Sub Enable_buttons()
        Dispatcher.BeginInvoke(Sub()
                                   poweroff.IsEnabled = True
                                   reboot.IsEnabled = True
                                   recovery.IsEnabled = True
                                   fastboot.IsEnabled = True
                                   list_read.IsEnabled = True
                                   list_write.IsEnabled = True
                                   list_erase.IsEnabled = True
                                   m_write.IsEnabled = True
                                   m_read.IsEnabled = True
                                   m_erase.IsEnabled = True
                                   fdl_exec.IsEnabled = False
                                   set_active_a.IsEnabled = True
                                   set_active_b.IsEnabled = True
                                   start_repart.IsEnabled = True
                                   blk_size.IsEnabled = True
                               End Sub)
    End Sub
    Private Sub Write_con(text As String)
        If con.Dispatcher.CheckAccess() Then
            ' 当前线程是 UI 线程，直接操作
            con.Content = text
        Else
            ' 跨线程调用，使用 Dispatcher
            con.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub()
                               con.Content = text
                           End Sub))
        End If
    End Sub
    Private Sub Write_mode(text As String)
        If mode.Dispatcher.CheckAccess() Then
            ' 当前线程是 UI 线程，直接操作
            mode.Content = text
        Else
            ' 跨线程调用，使用 Dispatcher
            mode.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub()
                               mode.Content = text
                           End Sub))
        End If
    End Sub
    Public Sub AppendToOutput(text As String)
        If txtOutput Is Nothing Then
            Debug.WriteLine("错误: 输出文本框未初始化")
            Return
        End If

        ' 确保在 UI 线程执行
        If txtOutput.Dispatcher.CheckAccess() Then
            ' 当前线程是 UI 线程，直接操作
            txtOutput.AppendText(text & Environment.NewLine)
            txtOutput.ScrollToEnd()
        Else
            ' 跨线程调用，使用 Dispatcher
            txtOutput.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub()
                               txtOutput.AppendText(text & Environment.NewLine)
                               txtOutput.ScrollToEnd()
                           End Sub))
        End If
    End Sub
    Public Sub AppendWithTimestamp(text As String)
        AppendToOutput($"[{DateTime.Now:HH:mm:ss}] {text}")
    End Sub

    ' 添加分隔线
    Public Sub AppendSeparator()
        AppendToOutput(New String("-"c, 80))
    End Sub
    Public Function ExportTextBoxContent(outputBox As TextBox) As Boolean
        Try
            ' 创建保存文件对话框
            Dim saveDialog As New SaveFileDialog()

            ' 设置对话框属性
            saveDialog.Title = "导出日志文件"
            saveDialog.Filter = "文本文件 (*.txt)|*.txt|日志文件 (*.log)|*.log|所有文件 (*.*)|*.*"
            saveDialog.FilterIndex = 1
            saveDialog.DefaultExt = ".txt"
            saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            saveDialog.FileName = $"Log0990_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            saveDialog.OverwritePrompt = True ' 覆盖提示

            ' 显示对话框
            If saveDialog.ShowDialog() <> True Then
                ' 用户取消
                AppendToOutput("导出已取消")
                Return False
            End If

            ' 获取文件路径
            Dim filePath As String = saveDialog.FileName

            ' 获取 TextBox 内容
            Dim content As String = GetTextBoxText(outputBox)

            ' 写入文件
            File.WriteAllText(filePath, content, Encoding.UTF8)

            ' 显示成功消息
            AppendToOutput($"日志已成功导出到: {filePath}")
            Return True

        Catch ex As Exception
            ' 错误处理
            AppendToOutput($"导出失败: {ex.Message}")
            Return False
        End Try
    End Function

    ' 安全获取 TextBox 内容（跨线程安全）
    Private Function GetTextBoxText(textBox As TextBox) As String
        If textBox.Dispatcher.CheckAccess() Then
            Return textBox.Text
        Else
            Dim result As String = String.Empty
            textBox.Dispatcher.Invoke(Sub() result = textBox.Text)
            Return result
        End If
    End Function

    ' 清空输出框
    Public Sub ClearOutput()
        If txtOutput Is Nothing Then Return

        If txtOutput.Dispatcher.CheckAccess() Then
            txtOutput.Clear()
        Else
            txtOutput.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                New Action(Sub() txtOutput.Clear()))
        End If
    End Sub



    Private Sub select_fdl_Click(sender As Object, e As RoutedEventArgs) Handles select_fdl.Click
        fdl_file_path.Text = ShowFileDialog()
    End Sub

    Private Sub select_cve_Click(sender As Object, e As RoutedEventArgs) Handles select_cve.Click
        cve_addr.Text = ShowFileDialog()
    End Sub
    Public Function HexStringToUInteger(hexString As String) As UInteger
        ' 移除可能存在的空格
        hexString = hexString.Trim()

        ' 检查并移除十六进制前缀
        If hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
            hexString = hexString.Substring(2)
        End If

        Try
            ' 将十六进制字符串转换为 UInteger
            Return Convert.ToUInt32(hexString, 16)
        Catch ex As FormatException
            Throw New ArgumentException($"无效的十六进制字符串: {hexString}", ex)
        Catch ex As OverflowException
            Throw New ArgumentException($"数值超出 UInteger 范围: {hexString}", ex)
        End Try
    End Function

    Public Function ShowFileDialog() As String
        ' 创建文件对话框实例
        Dim openFileDialog As New OpenFileDialog()

        ' 设置对话框属性
        openFileDialog.Title = "选择文件" ' 对话框标题
        openFileDialog.Filter = "所有文件 (*.*)|*.*|二进制文件 (*.bin)|*.bin|镜像文件 (*.img)|*.img" ' 文件过滤器
        openFileDialog.FilterIndex = 2 ' 默认选择第二个过滤器
        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) ' 初始目录
        openFileDialog.Multiselect = False ' 禁用多选

        ' 显示对话框并检查结果
        If openFileDialog.ShowDialog() = True Then
            ' 用户点击了"打开"
            Return openFileDialog.FileName
        Else
            ' 用户取消选择
            Return String.Empty
        End If
    End Function

    Private Sub exp_log_Click(sender As Object, e As RoutedEventArgs) Handles exp_log.Click
        ExportTextBoxContent(txtOutput)
    End Sub



    Private Sub select_list_addr_Click(sender As Object, e As RoutedEventArgs) Handles select_list_addr.Click
        list_file_addr.Text = ShowFileDialog()
    End Sub

    Private Sub poweroff_Click(sender As Object, e As RoutedEventArgs) Handles poweroff.Click
        utils.ShutdownDevice()
        Application.Current.Shutdown(0)
    End Sub

    Private Sub reboot_Click(sender As Object, e As RoutedEventArgs) Handles reboot.Click
        utils.PowerOnDevice()
    End Sub

    Private Sub recovery_Click(sender As Object, e As RoutedEventArgs) Handles recovery.Click
        utils.ResetToCustomMode(CustomModesToReset.Recovery)
    End Sub

    Private Sub fastboot_Click(sender As Object, e As RoutedEventArgs) Handles fastboot.Click
        utils.ResetToCustomMode(CustomModesToReset.Fastboot)
    End Sub
    Public Function OutImageFileDiag(name As String)
        Dim saveDialog As New SaveFileDialog()

        ' 设置对话框属性
        saveDialog.Title = "保存镜像文件"
        saveDialog.Filter = "镜像文件（*.img）| *.img"
        saveDialog.FilterIndex = 1
        saveDialog.DefaultExt = ".img"
        saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        saveDialog.FileName = $"{name}.img"
        saveDialog.OverwritePrompt = True ' 覆盖提示

        ' 显示对话框
        If saveDialog.ShowDialog() <> True Then
            ' 用户取消
            AppendToOutput("Cancelled")
            Return False
        Else
            Dim filePath As String = saveDialog.FileName
            Return filePath
        End If

        ' 获取文件路径

    End Function
    Public Sub UpdateProgressInvoke(Percentage As Integer)

        Dispatcher.BeginInvoke(Sub()
                                   progressBar_1.Value = Percentage
                                   AppendToOutput($"Progress: {Str(Percentage)}%")
                               End Sub)
        '    End Sub)
    End Sub
    Private Async Sub list_write_Click(sender As Object, e As RoutedEventArgs) Handles list_write.Click
        Dim name As String = partitionName
        Dim filepath As String = list_file_addr.Text
        If name Is Nothing Then
            Exit Sub
        End If
        Await Task.Run(Sub()
                           Try
                               Dim fs As FileStream = File.OpenRead(filepath)
                               utils.WritePartition(name, fs)

                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Success",
                                                                                                                                                                                                                                                                       .Content = $"Write partition {name} successfully!{Environment.NewLine} 刷写分区{name}成功！",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                           Catch ex As Exception
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Error",
                                                                                                                                                                                                                                                                       .Content = $"Exception: '{ex.Message}' Exiting... {Environment.NewLine} 发生错误：‘{ex.Message}’退出中...",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End Try

                       End Sub)
    End Sub

    Private Async Sub list_read_Click(sender As Object, e As RoutedEventArgs) Handles list_read.Click
        Dim name = partitionName
        Dim path = OutImageFileDiag(name)
        If Not path = False Then
            Await Task.Run(Sub()
                               Try
                                   Dim fs As FileStream = File.Create($"{path}")
                                   utils.ReadPartitionCustomize(fs, name, utils.GetPartitionSize(name))
                                   Dispatcher.BeginInvoke(Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                           .Title = "Success",
                                                                                                                                                                                                                                                                           .Content = $"Read partition {name} successfully!{Environment.NewLine} 读取分区{name}成功！",
                                                                                                                                                                                                                                                                           .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                           .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                       }
                                                              dialog.ShowAsync()

                                                          End Sub)
                               Catch ex As Exception
                                   Dispatcher.BeginInvoke(Sub()
                                                              Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                           .Title = "Error",
                                                                                                                                                                                                                                                                           .Content = $"Exception: '{ex.Message}' Exiting... {Environment.NewLine} 发生错误：‘{ex.Message}’退出中...",
                                                                                                                                                                                                                                                                           .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                           .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                       }
                                                              dialog.ShowAsync()

                                                          End Sub)
                                   Thread.Sleep(5000)
                                   Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                               End Try


                           End Sub)
        End If




    End Sub

    Private Async Sub list_erase_Click(sender As Object, e As RoutedEventArgs) Handles list_erase.Click
        Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                           .Title = "WARNING",
                                                                                                                                                                                                                                                                           .Content = $"THIS FEATURE IS ONLY AVAILABLE FOR ORIGINAL RECOVERY DEVICES!! Do you want to continue?{Environment.NewLine} 此功能仅限原厂RECOVERY设备使用此功能！！！是否继续？",
                                                                                                                                                                                                                                                                           .PrimaryButtonText = "Yes",
                                                                                                                                                                                                                                                                           .SecondaryButtonText = "No",
                                                                                                                                                                                                                                                                           .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                       }
        Dim result = Await dialog.ShowAsync()
        If result = ContentDialogResult.Primary Then
            Dim name = partitionName
            utils.ErasePartition(name, 30000)
        End If
    End Sub

    Private Async Sub m_write_Click(sender As Object, e As RoutedEventArgs) Handles m_write.Click
        Dim name = m_part_flash.Text
        Dim filepath = m_file_path.Text
        Await Task.Run(Sub()
                           Try
                               Dim fs As FileStream = File.OpenRead(filepath)
                               utils.WritePartition(name, fs)
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Success",
                                                                                                                                                                                                                                                                       .Content = $"Write partition {name} successfully!{Environment.NewLine} 刷写分区{name}成功！",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                           Catch ex As Exception
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Error",
                                                                                                                                                                                                                                                                       .Content = $"Exception: '{ex.Message}' Exiting... {Environment.NewLine} 发生错误：‘{ex.Message}’退出中...",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End Try


                       End Sub)
    End Sub

    Private Async Sub m_read_Click(sender As Object, e As RoutedEventArgs) Handles m_read.Click
        Dim name = m_part_read.Text
        Dim path = OutImageFileDiag(name)
        Await Task.Run(Sub()
                           Try
                               Dim fs As FileStream = File.Create(path)
                               utils.ReadPartitionCustomize(fs, name, utils.GetPartitionSize(name))
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Success",
                                                                                                                                                                                                                                                                       .Content = $"Read partition {name} successfully!{Environment.NewLine} 读取分区{name}成功！",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                           Catch ex As Exception
                               Dispatcher.BeginInvoke(Sub()
                                                          Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                       .Title = "Error",
                                                                                                                                                                                                                                                                       .Content = $"Exception: '{ex.Message}' Exiting... {Environment.NewLine} 发生错误：‘{ex.Message}’退出中...",
                                                                                                                                                                                                                                                                       .PrimaryButtonText = "OK",
                                                                                                                                                                                                                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                   }
                                                          dialog.ShowAsync()

                                                      End Sub)
                               Thread.Sleep(5000)
                               Dispatcher.BeginInvoke(Sub() Application.Current.Shutdown(1))
                           End Try
                       End Sub)
    End Sub

    Private Async Sub m_erase_Click(sender As Object, e As RoutedEventArgs) Handles m_erase.Click
        Dim dialog = New ContentDialog() With {
                                                                                                                                                                                                                                                                           .Title = "WARNING",
                                                                                                                                                                                                                                                                           .Content = $"THIS FEATURE IS ONLY AVAILABLE FOR ORIGINAL RECOVERY DEVICES!! Do you want to continue?{Environment.NewLine} 此功能仅限原厂RECOVERY设备使用此功能！！！是否继续？",
                                                                                                                                                                                                                                                                           .PrimaryButtonText = "Yes",
                                                                                                                                                                                                                                                                           .SecondaryButtonText = "No",
                                                                                                                                                                                                                                                                           .DefaultButton = ContentDialogButton.Primary
                                                                                                                                                                                                                                                                       }
        Dim result = Await dialog.ShowAsync()
        If result = ContentDialogResult.Primary Then
            Dim name = m_part_erase.Text
            utils.ErasePartition(name, 30000)
        End If
    End Sub

    Private Sub set_active_a_Click(sender As Object, e As RoutedEventArgs) Handles set_active_a.Click
        utils.SetActiveSlot(SlotToSetActive.SlotA)
        Dim dialog = New ContentDialog() With {
                                                                       .Title = "Set A parts command sent",
                                                                       .Content = $"Operation 'Set active: A parts'sent{Environment.NewLine} 操作‘设置活动分区: A分区’发送成功",
                                                                       .PrimaryButtonText = "OK",
                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                   }
        dialog.ShowAsync()
    End Sub

    Private Sub set_active_b_Click(sender As Object, e As RoutedEventArgs) Handles set_active_b.Click
        utils.SetActiveSlot(SlotToSetActive.SlotB)
        Dim dialog = New ContentDialog() With {
                                                                       .Title = "Set B parts command sent",
                                                                       .Content = $"Operation 'Set active: B parts'sent{Environment.NewLine} 操作‘设置活动分区: B分区’发送成功",
                                                                       .PrimaryButtonText = "OK",
                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                   }
        dialog.ShowAsync()
    End Sub

    Private Sub select_xml_Click(sender As Object, e As RoutedEventArgs) Handles select_xml.Click
        xml_path.Text = ShowFileDialog()
    End Sub

    Private Async Sub start_repart_Click(sender As Object, e As RoutedEventArgs) Handles start_repart.Click
        Dim XML_path_1 = xml_path.Text
        Dim repartlist As New List(Of Partition)
        Await Task.Run(Sub() LoadPartitionsFromXml(XML_path_1, repartlist))
        utils.Repartition(repartlist)
        Dim dialog = New ContentDialog() With {
                                                                       .Title = "Repartition command sent",
                                                                       .Content = $"Operation 'Repartition'sent{Environment.NewLine} 操作‘重新分区’发送成功",
                                                                       .PrimaryButtonText = "OK",
                                                                       .DefaultButton = ContentDialogButton.Primary
                                                                   }
        dialog.ShowAsync()
    End Sub

    Private Sub blk_size_ValueChanged(sender, e)
        Dispatcher.BeginInvoke(Sub()
                                   size_con.Content = Str(Math.Round(blk_size.Value))
                                   If FDL2_Executed Then
                                       ' Execute(Nothing, $"blk_size {Str(Math.Round(blk_size.Value))}")
                                       utils.PerBlockSize = CType(Math.Round(blk_size.Value), UShort）
                                   End If

                               End Sub)

    End Sub

    Private Sub log_clear_Click(sender As Object, e As RoutedEventArgs) Handles log_clear.Click
        txtOutput.Clear()
    End Sub

    Private Sub m_select_Click(sender As Object, e As RoutedEventArgs) Handles m_select.Click
        m_file_path.Text = ShowFileDialog()
    End Sub

    Private Sub part_list_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim index = part_list.SelectedIndex
        If index >= 0 Then
            Dim parts() As String = part_list.Items(index).ToString().Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            If parts.Length >= 2 Then
                partitionName = parts(2)
                MessageBox.Show($"Select:{partitionName}")
            Else
                partitionName = Nothing
            End If
        Else
            partitionName = String.Empty
        End If
    End Sub
End Class

