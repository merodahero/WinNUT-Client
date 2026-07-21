' WinNUT-Client is a NUT windows client for monitoring your ups hooked up to your favorite linux server.
' Copyright (C) 2019-2024 Gawindx (Decaux Nicolas)
'
' This program is free software: you can redistribute it and/or modify it under the terms of the
' GNU General Public License as published by the Free Software Foundation, either version 3 of the
' License, or any later version.
'
' This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

Imports System.ComponentModel
Imports System.Drawing.Drawing2D

Namespace Controls

    Friend Class UPSVarGauge
        Inherits CustomGauge

#Region "Private Fields"

        Private ReadOnly m_value1 As Single
        Private m_value2 As Single

        Private ReadOnly m_MinValue As Single = 0
        Private ReadOnly m_MaxValue As Single = 100

        Private ReadOnly m_BaseArcRadius = 45
        Private ReadOnly m_BaseArcStart = 135
        Private ReadOnly m_BaseArcSweep = 270
        Private ReadOnly m_BaseArcWidth = 5

        ' Private ReadOnly m_ScaleLinesMinorTicks = 9
        Private ReadOnly m_ScaleLinesMinorInnerRadius = 42
        Private ReadOnly m_ScaleLinesMinorOuterRadius = 48
        Private ReadOnly m_ScaleLinesMinorWidth = 1

        Private ReadOnly m_ScaleLinesInterInnerRadius = 40
        Private ReadOnly m_ScaleLinesInterOuterRadius = 48
        Private ReadOnly m_ScaleLinesInterWidth = 1

        ' Private ReadOnly m_ScaleLinesMajorStepValue = 50.0F
        Private ReadOnly m_ScaleLinesMajorInnerRadius = 40
        Private ReadOnly m_ScaleLinesMajorOuterRadius = 48
        Private ReadOnly m_ScaleLinesMajorWidth = 2

        Private ReadOnly m_ScaleNumbersRadius = 60
        Private ReadOnly m_ScaleNumbersFormat As String
        Private ReadOnly m_ScaleNumbersStartScaleLine As Integer
        Private ReadOnly m_ScaleNumbersStepScaleLines = 1
        Private ReadOnly m_ScaleNumbersRotation As Integer

        Private ReadOnly m_NeedleRadius = 32
        Private ReadOnly m_NeedleColor1 = Color.Gray
        Private ReadOnly m_NeedleColor2 = Color.DimGray
        Private ReadOnly m_NeedleWidth = 2

        Private m_gradientType = GradientTypeEnum.RedGreen
        Private m_gradientOrientation = GradientOrientationEnum.BottomToTop
        Private m_unitvalue1 = UnitValueEnum.Volts
        Private m_unitvalue2 = UnitValueEnum.None

#End Region

#Region "Properties"

        ' Map Value1 onto Value
        <Browsable(True),
                Category("AGauge"),
                Description("First value to display.")>
        Public Property Value1 As Single
            Get
                Return MyBase.Value
            End Get
            Set(value As Single)
                MyBase.Value = value
            End Set
        End Property

        <Browsable(True),
                Category("AGauge"),
                Description("Second value to display.")>
        Public Property Value2 As Single
            Get
                Return m_value2
            End Get
            Set(value As Single)
                If m_value2 <> value Then
                    m_value2 = value
                    OnValueChanged(Me, EventArgs.Empty)
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Browsable(True),
                Category("AGauge"),
                Description("UseColor For Arc Base Color.")>
        Public Property GradientType As GradientTypeEnum
            Get
                Return m_gradientType
            End Get
            Set(value As GradientTypeEnum)
                m_gradientType = value
                Me.Invalidate()
            End Set
        End Property

        <Browsable(True),
                Category("AGauge"),
                Description("Orientation Of Gradient Colors.")>
        Public Property GradientOrientation As GradientOrientationEnum
            Get
                Return m_gradientOrientation
            End Get
            Set(value As GradientOrientationEnum)

                If m_gradientOrientation <> value Then
                    m_gradientOrientation = value
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Browsable(True),
                Category("AGauge"),
                Description("Units For Value 1")>
        Public Property UnitValue1 As UnitValueEnum
            Get
                Return m_unitvalue1
            End Get
            Set(value As UnitValueEnum)

                If m_unitvalue1 <> value Then
                    m_unitvalue1 = value
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Browsable(True),
                Category("AGauge"),
                Description("UseColor For Arc Base Color.")>
        Public Property UnitValue2 As UnitValueEnum
            Get
                Return m_unitvalue2
            End Get
            Set(value As UnitValueEnum)

                If m_unitvalue2 <> value Then
                    m_unitvalue2 = value
                    Me.Invalidate()
                End If
            End Set
        End Property

#End Region

        Public Enum GradientTypeEnum
            None
            RedGreen
        End Enum

        Public Enum GradientOrientationEnum
            TopToBottom
            BottomToTop
            RightToLeft
            LeftToRight
        End Enum

        Public Enum UnitValueEnum
            None
            Hertz
            Percent
            Volts
            Watts
        End Enum

        Public Sub New()
            MyBase.New()
            ' Initialize component if it exists, but don't fail if it doesn't
            Try
                InitializeComponent()
            Catch ex As Exception
                ' Designer component initialization is optional for this control
            End Try
        End Sub

        Protected Overrides Sub RenderDefaultArc(graphics As Graphics)
            Try
                ' Use the base class properties
                If BaseArcRadius > 0 AndAlso centerFactor > 0 Then
                    Dim baseArcRadius As Integer = CInt(BaseArcRadius * centerFactor)
                    Dim scaledWidth As Single = BaseArcWidth * centerFactor

                    ' Create the rectangle for the arc
                    Dim rect As New Rectangle(Center.X - baseArcRadius,
                                              Center.Y - baseArcRadius,
                                              2 * baseArcRadius,
                                              2 * baseArcRadius)

                    ' ALWAYS use gradient - ignore the GradientType setting for now to test
                    ' Create gradient brush with simple vertical gradient
                    If rect.Width > 1 AndAlso rect.Height > 1 Then
                        Using brush As New LinearGradientBrush(rect, Color.Red, Color.Lime, 90.0F)
                            Using pnArc = New Pen(brush, scaledWidth)
                                graphics.DrawArc(pnArc, rect, 135, 270)
                            End Using
                        End Using
                    Else
                        ' Fallback if rect is too small
                        Using pnArc = New Pen(Color.Red, scaledWidth)
                            graphics.DrawArc(pnArc, rect, 135, 270)
                        End Using
                    End If
                End If
            Catch ex As Exception
                ' If anything fails, draw a bright red arc so we know something happened
                Try
                    If BaseArcRadius > 0 AndAlso centerFactor > 0 Then
                        Dim baseArcRadius As Integer = CInt(BaseArcRadius * centerFactor)
                        Dim scaledWidth As Single = BaseArcWidth * centerFactor
                        Dim rect As New Rectangle(Center.X - baseArcRadius,
                                                  Center.Y - baseArcRadius,
                                                  2 * baseArcRadius,
                                                  2 * baseArcRadius)
                        Using pnArc = New Pen(Color.Magenta, scaledWidth)
                            graphics.DrawArc(pnArc, rect, 135, 270)
                        End Using
                    End If
                Catch
                    ' Silently fail
                End Try
            End Try
        End Sub

        ''' <summary>
        ''' Override PostRender and render the value of the gauge with unit.
        ''' </summary>
        Protected Overrides Sub PostRender(graphics As Graphics)
            Dim PenString = New Pen(Color.Black)
            Dim PenFontV1 = New Font("Microsoft Sans Serif", 8, FontStyle.Bold)
            Dim PenFontV2 = New Font("Microsoft Sans Serif", 7, FontStyle.Bold)
            Dim StringPen = New SolidBrush(Color.Black)
            Dim LineHeight = 15
            Dim StrPos = Center
            StrPos.Y += 5

            If UnitValue1 <> UnitValueEnum.None Then
                Dim StringToDraw = ApplyUnit(Value1, UnitValue1)
                Dim StringSize = TextRenderer.MeasureText(StringToDraw, PenFontV1)
                StrPos.Y += LineHeight
                graphics.DrawString(StringToDraw, PenFontV1, StringPen,
                                        New PointF((StrPos.X - (StringSize.Width / 2) + 5), StrPos.Y))
            End If

            If UnitValue2 <> UnitValueEnum.None Then
                Dim StringToDraw = ApplyUnit(Value2, UnitValue2)
                Dim StringSize = TextRenderer.MeasureText(StringToDraw, PenFontV2)
                StrPos.Y += LineHeight
                graphics.DrawString(StringToDraw, PenFontV2, StringPen,
                                        New PointF((StrPos.X - (StringSize.Width / 2) + 7), StrPos.Y))
            End If
        End Sub

        Private Function ApplyUnit(value As Single, unit As UnitValueEnum) As String
            Dim returnStr = value.ToString("F1")

            Select Case unit
                Case UnitValueEnum.Hertz
                    returnStr &= " Hz"
                Case UnitValueEnum.Percent
                    returnStr &= " %"
                Case UnitValueEnum.Volts
                    returnStr &= " V"
                Case UnitValueEnum.Watts
                    returnStr &= " W"
            End Select

            Return returnStr
        End Function

    End Class

End Namespace
