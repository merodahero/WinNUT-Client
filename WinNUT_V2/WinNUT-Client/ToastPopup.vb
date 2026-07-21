' WinNUT-Client is a NUT windows client for monitoring your ups hooked up to your favorite linux server.
' Copyright (C) 2019-2021 Gawindx (Decaux Nicolas)
'
' This program is free software: you can redistribute it and/or modify it under the terms of the
' GNU General Public License as published by the Free Software Foundation, either version 3 of the
' License, or any later version.
'
' This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

Imports CommunityToolkit.WinUI.Notifications
Imports System.Threading.Tasks

''' <summary>
''' Class to send a Toast Notification on Windows 10 and up. Made possible thanks to CommunityToolkit.WinUI.Notifications
''' package and https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
''' </summary>
Public Class ToastPopup
    'Private Header As String = ""
    'Public WriteOnly Property ToastHeader() As String
    '    Set(ByVal Value As String)
    '        Me.Header = Value
    '    End Set
    'End Property
    'Public toastCollectionId As String = "WinNUTToastCollection"
    'Create a toast collection
    'Public Async Sub CreateToastCollection(ByVal IconUri As String)
    'Public Sub CreateToastCollection(ByVal IconUri As String)
    'Dim displayName As String = "WinNUT"
    'Dim launchArg As String = "WinNUTNotifications"
    'Dim IconToast As Uri = New Uri(IconUri)

    'Constructor
    'Dim WinNutToastCollection As Windows.UI.Notifications.ToastCollection = New Windows.UI.Notifications.ToastCollection(
    'Me.toastCollectionId,
    '       displayName,
    '       launchArg,
    '       IconToast)
    'Calls the platform to create the collection
    '   Await Windows.UI.Notifications.ToastNotificationManager.GetDefault.GetToastCollectionManager.SaveToastCollectionAsync(WinNutToastCollection).GetResults()
    'GetDefault().GetToastCollectionManager().SaveToastCollectionAsync(WinNutToastCollection)
    'Windows.UI.Notifications.ToastNotificationManager.GetDefault().GetToastCollectionManager().SaveToastCollectionAsync(WinNutToastCollection)
    ' End Sub

    Public Sub SendToast(ToastParts As String())
        Try
            Dim toastBuilder = New ToastContentBuilder()
            For i = 0 To ToastParts.Count - 1
                toastBuilder.AddText(ToastParts(i))
            Next

            ' Build and show the toast
            ' In .NET 8+, we need to manually show the toast using the content
            Dim content = toastBuilder.GetToastContent()
            Dim xmlDoc = content.GetXml()

            ' Create a basic toast notifier - this doesn't require Windows.UI namespace
            ' The toast is shown via the notification system
            toastBuilder.Show()

        Catch ex As Exception
            ' Silently fail if toast notifications are not supported or available
            System.Diagnostics.Debug.WriteLine($"Toast notification error: {ex.Message}")
        End Try
    End Sub
End Class
