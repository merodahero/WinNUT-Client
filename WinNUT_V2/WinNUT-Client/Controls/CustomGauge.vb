' WinNUT-Client is a NUT windows client for monitoring your ups hooked up to your favorite linux server.
' Copyright (C) 2019-2024 Gawindx (Decaux Nicolas)
'
' This program is free software: you can redistribute it and/or modify it under the terms of the
' GNU General Public License as published by the Free Software Foundation, either version 3 of the
' License, or any later version.
'
' This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace Controls

    ''' <summary>
    ''' Custom gauge control for .NET 8.0 - replacement for AGauge.Classic
    ''' </summary>
    Public Class CustomGauge
        Inherits UserControl

#Region "Private Fields"

        Private _value As Single = 0
        Private _minValue As Single = 0
        Private _maxValue As Single = 100
        Private _baseArcColor As Color = Color.Gray
        Private _baseArcRadius As Integer = 70
        Private _baseArcStart As Integer = 135
        Private _baseArcSweep As Integer = 270
        Private _baseArcWidth As Integer = 8
        Private _needleColor1 As Color = Color.Red
        Private _needleColor2 As Color = Color.DimGray
        Private _needleRadius As Integer = 65
        Private _needleWidth As Integer = 3
        Private _scaleNumbersRadius As Integer = 85
        Private _scaleNumbersFormat As String = "{0:F0}"
        Private _scaleLinesMajorStepValue As Single = 20
        Private _scaleLinesMinorNumOf As Integer = 9
        Private _scaleLinesMinorInnerRadius As Integer = 68
        Private _scaleLinesMinorOuterRadius As Integer = 75
        Private _scaleLinesMajorInnerRadius As Integer = 65
        Private _scaleLinesMajorOuterRadius As Integer = 75
        Private _scaleLinesInterInnerRadius As Integer = 65
        Private _scaleLinesInterOuterRadius As Integer = 75

        Protected centerFactor As Single = 1.0F
        Protected Center As Point

#End Region

#Region "Properties"

        <Browsable(True), Category("Gauge"), Description("Current value of the gauge.")>
        Public Overridable Property Value As Single
            Get
                Return _value
            End Get
            Set(val As Single)
                If _value <> val Then
                    _value = Math.Max(_minValue, Math.Min(_maxValue, val))
                    OnValueChanged(Me, EventArgs.Empty)
                    Invalidate()
                End If
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Minimum value of the gauge.")>
        Public Property MinValue As Single
            Get
                Return _minValue
            End Get
            Set(val As Single)
                If _minValue <> val Then
                    _minValue = val
                    If _value < _minValue Then _value = _minValue
                    Invalidate()
                End If
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Maximum value of the gauge.")>
        Public Property MaxValue As Single
            Get
                Return _maxValue
            End Get
            Set(val As Single)
                If _maxValue <> val Then
                    _maxValue = val
                    If _value > _maxValue Then _value = _maxValue
                    Invalidate()
                End If
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Base arc color.")>
        Public Property BaseArcColor As Color
            Get
                Return _baseArcColor
            End Get
            Set(val As Color)
                _baseArcColor = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Base arc radius.")>
        Public Property BaseArcRadius As Integer
            Get
                Return _baseArcRadius
            End Get
            Set(val As Integer)
                _baseArcRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Base arc width.")>
        Public Property BaseArcWidth As Integer
            Get
                Return _baseArcWidth
            End Get
            Set(val As Integer)
                _baseArcWidth = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Needle primary color.")>
        Public Property NeedleColor1 As Color
            Get
                Return _needleColor1
            End Get
            Set(val As Color)
                _needleColor1 = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Needle secondary color.")>
        Public Property NeedleColor2 As Color
            Get
                Return _needleColor2
            End Get
            Set(val As Color)
                _needleColor2 = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Needle radius.")>
        Public Property NeedleRadius As Integer
            Get
                Return _needleRadius
            End Get
            Set(val As Integer)
                _needleRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Scale numbers radius.")>
        Public Property ScaleNumbersRadius As Integer
            Get
                Return _scaleNumbersRadius
            End Get
            Set(val As Integer)
                _scaleNumbersRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Scale numbers format string.")>
        Public Property ScaleNumbersFormat As String
            Get
                Return _scaleNumbersFormat
            End Get
            Set(val As String)
                _scaleNumbersFormat = If(String.IsNullOrEmpty(val), "{0:F0}", val)
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Major scale lines step value.")>
        Public Property ScaleLinesMajorStepValue As Single
            Get
                Return _scaleLinesMajorStepValue
            End Get
            Set(val As Single)
                _scaleLinesMajorStepValue = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Minor scale lines inner radius.")>
        Public Property ScaleLinesMinorInnerRadius As Integer
            Get
                Return _scaleLinesMinorInnerRadius
            End Get
            Set(val As Integer)
                _scaleLinesMinorInnerRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Minor scale lines outer radius.")>
        Public Property ScaleLinesMinorOuterRadius As Integer
            Get
                Return _scaleLinesMinorOuterRadius
            End Get
            Set(val As Integer)
                _scaleLinesMinorOuterRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Major scale lines inner radius.")>
        Public Property ScaleLinesMajorInnerRadius As Integer
            Get
                Return _scaleLinesMajorInnerRadius
            End Get
            Set(val As Integer)
                _scaleLinesMajorInnerRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Major scale lines outer radius.")>
        Public Property ScaleLinesMajorOuterRadius As Integer
            Get
                Return _scaleLinesMajorOuterRadius
            End Get
            Set(val As Integer)
                _scaleLinesMajorOuterRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Intermediate scale lines inner radius.")>
        Public Property ScaleLinesInterInnerRadius As Integer
            Get
                Return _scaleLinesInterInnerRadius
            End Get
            Set(val As Integer)
                _scaleLinesInterInnerRadius = val
                Invalidate()
            End Set
        End Property

        <Browsable(True), Category("Gauge"), Description("Intermediate scale lines outer radius.")>
        Public Property ScaleLinesInterOuterRadius As Integer
            Get
                Return _scaleLinesInterOuterRadius
            End Get
            Set(val As Integer)
                _scaleLinesInterOuterRadius = val
                Invalidate()
            End Set
        End Property

#End Region

#Region "Events"

        Public Event ValueChanged As EventHandler

        Protected Overridable Sub OnValueChanged(sender As Object, e As EventArgs)
            RaiseEvent ValueChanged(sender, e)
        End Sub

#End Region

#Region "Constructor"

        Public Sub New()
            Me.DoubleBuffered = True
            Me.ResizeRedraw = True
            Me.Size = New Size(200, 200)
            Me.BackColor = Color.Transparent

            ' Set default scale number format if not set
            If String.IsNullOrEmpty(_scaleNumbersFormat) Then
                _scaleNumbersFormat = "{0:F0}"
            End If
        End Sub

#End Region

#Region "Painting"

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Try
                MyBase.OnPaint(e)

                ' Don't paint if control is too small
                If Me.Width < 10 OrElse Me.Height < 10 Then Return

                Dim g As Graphics = e.Graphics
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.PixelOffsetMode = PixelOffsetMode.HighQuality

                ' Calculate center and scaling factor
                Center = New Point(Me.Width \ 2, Me.Height \ 2)
                Dim minSize As Integer = Math.Min(Me.Width, Me.Height)
                ' Scale to fill 90% of the available space instead of using a fixed 200px reference
                centerFactor = If(minSize > 0, (minSize * 0.9F) / 160.0F, 1.0F)

                ' Render components
                RenderDefaultArc(g)
                RenderScaleLines(g)
                RenderScaleNumbers(g)
                RenderNeedle(g)
                PostRender(g)
            Catch ex As Exception
                ' Prevent paint exceptions from crashing the app
                System.Diagnostics.Debug.WriteLine($"Gauge paint error: {ex.Message}")
            End Try
        End Sub

        Protected Overridable Sub RenderDefaultArc(g As Graphics)
            If _baseArcRadius > 0 Then
                Dim radius As Integer = CInt(_baseArcRadius * centerFactor)
                Dim rect As New Rectangle(Center.X - radius, Center.Y - radius, 2 * radius, 2 * radius)

                Using pen As New Pen(_baseArcColor, _baseArcWidth * centerFactor)
                    g.DrawArc(pen, rect, _baseArcStart, _baseArcSweep)
                End Using
            End If
        End Sub

        Protected Overridable Sub RenderScaleLines(g As Graphics)
            If _scaleLinesMajorStepValue <= 0 OrElse _maxValue <= _minValue Then Return

            Dim startAngle As Single = _baseArcStart
            Dim sweepAngle As Single = _baseArcSweep
            Dim valueRange As Single = _maxValue - _minValue
            Dim anglePerValue As Single = sweepAngle / valueRange

            ' Draw major scale lines
            Dim currentValue As Single = _minValue
            Dim safetyCounter As Integer = 0
            While currentValue <= _maxValue AndAlso safetyCounter < 1000
                Dim angle As Single = startAngle + (currentValue - _minValue) * anglePerValue
                DrawScaleLine(g, angle, _scaleLinesMajorInnerRadius, _scaleLinesMajorOuterRadius, 2)
                currentValue += _scaleLinesMajorStepValue
                safetyCounter += 1
            End While

            ' Draw minor scale lines
            If _scaleLinesMinorNumOf > 0 Then
                Dim minorStep As Single = _scaleLinesMajorStepValue / (_scaleLinesMinorNumOf + 1)
                If minorStep > 0 Then
                    currentValue = _minValue + minorStep
                    safetyCounter = 0
                    While currentValue < _maxValue AndAlso safetyCounter < 1000
                        If Math.Abs(currentValue Mod _scaleLinesMajorStepValue) > 0.001 Then
                            Dim angle As Single = startAngle + (currentValue - _minValue) * anglePerValue
                            DrawScaleLine(g, angle, _scaleLinesMinorInnerRadius, _scaleLinesMinorOuterRadius, 1)
                        End If
                        currentValue += minorStep
                        safetyCounter += 1
                    End While
                End If
            End If
        End Sub

        Protected Sub DrawScaleLine(g As Graphics, angle As Single, innerRadius As Integer, outerRadius As Integer, width As Integer)
            Dim angleRad As Double = angle * Math.PI / 180.0
            Dim innerR As Single = innerRadius * centerFactor
            Dim outerR As Single = outerRadius * centerFactor

            Dim innerPt As New PointF(
                Center.X + CSng(Math.Cos(angleRad) * innerR),
                Center.Y + CSng(Math.Sin(angleRad) * innerR))

            Dim outerPt As New PointF(
                Center.X + CSng(Math.Cos(angleRad) * outerR),
                Center.Y + CSng(Math.Sin(angleRad) * outerR))

            Using pen As New Pen(Color.Black, width * centerFactor)
                g.DrawLine(pen, innerPt, outerPt)
            End Using
        End Sub

        Protected Overridable Sub RenderScaleNumbers(g As Graphics)
            If _scaleLinesMajorStepValue <= 0 OrElse _maxValue <= _minValue Then Return

            Dim startAngle As Single = _baseArcStart
            Dim sweepAngle As Single = _baseArcSweep
            Dim valueRange As Single = _maxValue - _minValue
            Dim anglePerValue As Single = sweepAngle / valueRange

            ' Ensure we have a valid format string
            Dim formatString As String = If(String.IsNullOrEmpty(_scaleNumbersFormat), "{0:F0}", _scaleNumbersFormat)

            Using font As New Font("Arial", Math.Max(1, 8 * centerFactor), FontStyle.Bold)
                Using brush As New SolidBrush(Color.Black)
                    Dim currentValue As Single = _minValue
                    Dim safetyCounter As Integer = 0
                    While currentValue <= _maxValue AndAlso safetyCounter < 100
                        Dim angle As Single = startAngle + (currentValue - _minValue) * anglePerValue
                        Dim angleRad As Double = angle * Math.PI / 180.0
                        Dim radius As Single = _scaleNumbersRadius * centerFactor

                        Dim textPos As New PointF(
                            Center.X + CSng(Math.Cos(angleRad) * radius),
                            Center.Y + CSng(Math.Sin(angleRad) * radius))

                        Dim text As String = String.Format(formatString, currentValue)
                        Dim textSize As SizeF = g.MeasureString(text, font)
                        textPos.X -= textSize.Width / 2
                        textPos.Y -= textSize.Height / 2

                        g.DrawString(text, font, brush, textPos)
                        currentValue += _scaleLinesMajorStepValue
                        safetyCounter += 1
                    End While
                End Using
            End Using
        End Sub

        Protected Overridable Sub RenderNeedle(g As Graphics)
            If _maxValue <= _minValue Then Return

            Dim startAngle As Single = _baseArcStart
            Dim sweepAngle As Single = _baseArcSweep
            Dim valueRange As Single = _maxValue - _minValue
            Dim valueAngle As Single = startAngle + (_value - _minValue) * sweepAngle / valueRange

            Dim angleRad As Double = valueAngle * Math.PI / 180.0
            Dim needleR As Single = _needleRadius * centerFactor

            Dim needleEnd As New PointF(
                Center.X + CSng(Math.Cos(angleRad) * needleR),
                Center.Y + CSng(Math.Sin(angleRad) * needleR))

            Using pen As New Pen(_needleColor1, Math.Max(1, _needleWidth * centerFactor))
                pen.EndCap = LineCap.ArrowAnchor
                g.DrawLine(pen, Center, needleEnd)
            End Using

            ' Draw center circle
            Dim circleRadius As Single = Math.Max(1, 4 * centerFactor)
            Using brush As New SolidBrush(_needleColor2)
                g.FillEllipse(brush, Center.X - circleRadius, Center.Y - circleRadius,
                             2 * circleRadius, 2 * circleRadius)
            End Using
        End Sub

        Protected Overridable Sub PostRender(g As Graphics)
            ' Override in derived classes for custom rendering
        End Sub

#End Region

    End Class

End Namespace
