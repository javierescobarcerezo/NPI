﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System.Windows.Media.Imaging;
    using System.Linq;
    using System.Globalization;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Pen penVerde = new Pen(Brushes.Green, 6);
        private readonly Brush puntoVerde = Brushes.GreenYellow;

        private readonly Pen penAmarillo = new Pen(Brushes.Yellow, 6);
        private readonly Brush puntoAmarillo = Brushes.Yellow;

        private readonly Pen penCyan = new Pen(Brushes.Cyan, 6);
        private readonly Brush puntoCyan = Brushes.Cyan;


        private readonly Pen penRojo = new Pen(Brushes.Red, 6);
        private readonly Brush puntoRojo = Brushes.Red;


        private readonly Pen drawPen = new Pen(Brushes.Blue, 6);

        //flags

        /// <summary>
        /// Controla si debemos de levantar los brazos o bajarlos
        /// </summary>
        private bool bajar_brazos = false;

        /// <summary>
        /// Se ha implementado para evitar que se pueda escribir una variable más de una vez en poco tiempo
        /// </summary>
        private bool escrito = false; //Mientras sea false se puede cambiar un contador

        /// <summary>
        /// Contiene el valor de la longitud de los brazos del usuario
        /// </summary>
        private double longitud_brazos= 0;

        /// <summary>
        /// Variable para controlar qué se pinta y qué no
        /// </summary>
        private bool pintar= true; //pintar arriba los puntos if true

        Point obj_der_sup; //Punto objetivo derecho
        Point obj_izq_sup; //Punto objetivo izquierdo
        Point obj_der_inf; //Punto objetivo derecho
        Point obj_izq_inf; //Punto objetivo izquierdo

        /// <summary>
        /// Acumulador provisional para asegurar que una postura se mantiene durante un tiempo
        /// </summary>
        private int acumulador = 0;

        /// <summary>
        /// Variable para guardar puntos de referencia (provisional)
        /// </summary>
        private float referencia;

        /// <summary>
        /// Porcentaje de error en los ejercicios, predeterminado a 5%
        /// </summary>
        private double error = 5.0;

        private int n_error = 0;

        /// <summary>
        /// Si true se realiza este ejercicio
        /// </summary>
        private bool ejercicio1 = false;

        /// <summary>
        /// Si true se realiza este ejercicio
        /// </summary>
        private bool ejercicio2 = false;

        /// <summary>
        /// Si true se realiza este ejercicio
        /// </summary>
        private bool ejercicio3 = false;

        private bool parte1 = false;
        private bool parte2 = false;
        private bool parte3 = false;


        /// <summary>
        /// Número de repeticiones de un ejercicio
        /// </summary>
        private int repeticiones_totales = 3;
        private int repeticiones = 0;

        /// <summary>
        /// Array de movimientos (provisional)
        /// </summary>
        private bool[] movimiento;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 5;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen trackedBonePen = new Pen(Brushes.Red, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;
            SkeletonImage.Source = this.imageSource;



            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.referencias.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {           

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {

            if (ejercicio1) {
                itinerario1(skeleton, drawingContext);
            }
            

            //Aquí se añadirían más ejercicios

            /*// Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }*/
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1, Pen pen)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = pen;
            }


            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        } 

        /// <summary>
        /// Boolean method that return true if body is completely aligned and arms are in a relaxed position
        /// </summary>
        /// <param name="received"></param>
        /// <returns></returns>
        private bool IsAlignedBodyAndArms(Skeleton received)
        {
            double HipCenterPosX = received.Joints[JointType.HipCenter].Position.X;
            double HipCenterPosY = received.Joints[JointType.HipCenter].Position.Y;
            double HipCenterPosZ = received.Joints[JointType.HipCenter].Position.Z;

            double ShoulCenterPosX = received.Joints[JointType.ShoulderCenter].Position.X;
            double ShoulCenterPosY = received.Joints[JointType.ShoulderCenter].Position.Y;
            double ShoulCenterPosZ = received.Joints[JointType.ShoulderCenter].Position.Z;

            double HeadCenterPosX = received.Joints[JointType.Head].Position.X;
            double HeadCenterPosY = received.Joints[JointType.Head].Position.Y;
            double HeadCenterPosZ = received.Joints[JointType.Head].Position.Z;

            double ElbLPosX = received.Joints[JointType.ElbowLeft].Position.X;
            double ElbLPosY = received.Joints[JointType.ElbowLeft].Position.Y;

            double ElbRPosX = received.Joints[JointType.ElbowRight].Position.X;
            double ElbRPosY = received.Joints[JointType.ElbowRight].Position.Y;

            double WriLPosX = received.Joints[JointType.WristLeft].Position.X;
            double WriLPosY = received.Joints[JointType.WristLeft].Position.Y;
            double WriLPosZ = received.Joints[JointType.WristLeft].Position.Z;

            double WriRPosX = received.Joints[JointType.WristRight].Position.X;
            double WriRPosY = received.Joints[JointType.WristRight].Position.Y;
            double WriRPosZ = received.Joints[JointType.WristRight].Position.Z;

            double ShouLPosX = received.Joints[JointType.ShoulderLeft].Position.X;
            double ShouLPosY = received.Joints[JointType.ShoulderLeft].Position.Y;
            double ShouLPosZ = received.Joints[JointType.ShoulderLeft].Position.Z;

            double ShouRPosX = received.Joints[JointType.ShoulderRight].Position.X;
            double ShouRPosY = received.Joints[JointType.ShoulderRight].Position.Y;
            double ShouRPosZ = received.Joints[JointType.ShoulderRight].Position.Z;

            //have to change to correspond to the 5% error
            //distance from Shoulder to Wrist for the projection in line with shoulder
            double distShouLtoWristL = ShouLPosY - WriLPosY;
            //caldulate admited error 5% that correspond to 9 degrees for each side
            double radian = (9 * Math.PI) / 180;
            double DistErrorL = distShouLtoWristL * Math.Tan(radian);

            double distShouLtoWristR = ShouRPosY - WriRPosY;
            //caldulate admited error 5% that correspond to 9 degrees for each side

            double DistErrorR = distShouLtoWristR * Math.Tan(radian);
            //double ProjectionWristX = ShouLPosX;
            //double ProjectionWristZ = WriLPosZ;

            //determine of projected point from shoulder to wrist LEFT and RIGHT and then assume error
            double ProjectedPointWristLX = ShouLPosX;
            double ProjectedPointWristLY = WriLPosY;
            double ProjectedPointWristLZ = ShouLPosZ;

            double ProjectedPointWristRX = ShouRPosX;
            double ProjectedPointWristRY = WriRPosY;
            double ProjectedPointWristRZ = ShouRPosZ;


            //Create method to verify if the center of the body is completely aligned
            //head with shoulder center and with hip center
            if (Math.Abs(HeadCenterPosX - ShoulCenterPosX) <= 0.05 && Math.Abs(ShoulCenterPosX - HipCenterPosX) <= 0.05)
            {
                //if position of left wrist is between [ProjectedPointWrist-DistError,ProjectedPointWrist+DistError]
                if (Math.Abs(WriLPosX - ProjectedPointWristLX) <= DistErrorL && Math.Abs(WriRPosX - ProjectedPointWristRX) <= DistErrorR)
                {
                    return true;
                }
                else return false;
            }
            else return false;

        }

        /// <summary>
        /// Handler for click event from "Itinerario1_Click" button
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void Itinerario1_Click(object sender, RoutedEventArgs e) {
            if (null == this.sensor)
            {
                return;
            }
            Descrip_mov.Text = "Póngase en posición relajada y los brazos pegados al cuerpo. A continuación suba los brazos hasta ponerlos en cruz. Repita ";
            Descrip_mov.Foreground = Brushes.DarkBlue;
            ejercicio1 = true;
            parte1 = true;
            Itinerario1.Visibility = Visibility.Hidden;
            Itinerario2.Visibility = Visibility.Hidden;
            Itinerario3.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        /// <summary>
        /// Ejecuta la segunda parte del ejercicio 1. Movimiento basado en la posicion 1 y 4
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private int ejerc1_parte2(Skeleton skeleton) {

            double p_error = error / 100; // Cambiamos la forma de representar error para usarla más fácilmente

            // Conteo de errores 
            /*if ((mano_derecha * (1.20) > skeleton.Joints[JointType.HandRight].Position.Y) && !bajar_brazos)
            {
                return -1;
            }
            if ((mano_izquierda * (1.20) > skeleton.Joints[JointType.HandLeft].Position.Y) && !bajar_brazos)
            {
                return -1;
            }*/
            
            if (//Comprobamos que los brazos están en cruz
                    //Comprueba que el brazo izquierdo esté posicionado correctamente
                (skeleton.Joints[JointType.HandLeft].Position.Z * (1.0 + p_error) > skeleton.Joints[JointType.ShoulderLeft].Position.Z) &&
                (skeleton.Joints[JointType.HandLeft].Position.Z * (1.0 - p_error) < skeleton.Joints[JointType.ShoulderLeft].Position.Z) &&
                (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X) &&
                (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.WristLeft].Position.X) &&

                    //Comprueba que el brazo derecho esté posicionado correctamente
                (skeleton.Joints[JointType.HandRight].Position.Z * (1.0 + p_error) > skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.Z * (1.0 - p_error) < skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.WristRight].Position.X) &&
                    //Comprueba que ambas manos estén al nivel de los hombros
                (skeleton.Joints[JointType.HandRight].Position.Y * (1.0 + p_error) > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandRight].Position.Y * (1.0 - p_error) < skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y * (1.0 + p_error) > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y * (1.0 - p_error) < skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                !bajar_brazos
                )
            {
                escrito = false; // Permitimos que se pueda actualiza la variable repeticiones que lleva las repeticiones
                pintar = false; // Cambiamos el patrón de pintado, si false se pintaran los puntos de refencia inferiores
                bajar_brazos = true; // Ahora deben bajarse los brazos
                Instruccion.Text = "Ahora baje los brazos";
            }
            // Si cierto, se toma una iteración del ejercicio como completada y se cambia el patrón de pintado de referencia
            if (bajar_brazos && IsAlignedBodyAndArms(skeleton))
            {
                pintar = true;
                return 1;
            }
            else
                return 0;
        }

        /// <summary>
        /// Ejecuta el itinerario 1
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="drawingContext"></param>
        private void itinerario1(Skeleton skeleton, DrawingContext drawingContext)
        {

            error = Regulador.Value;
            valor_error.Content = error.ToString();

            Point puntoAux4 = this.SkeletonPointToScreen(skeleton.Joints[JointType.ShoulderRight].Position);
            Point puntoAux5 = this.SkeletonPointToScreen(skeleton.Joints[JointType.ShoulderLeft].Position);

            if (ejercicio1)
            {
                Repeticiones.Text = repeticiones.ToString(); // Mostramos las repeticiones actuales a hacer por pantalla
                //Primera parte: comprobamos que estamos en posición de inicio, relajada
                if (parte1)
                {
                    if (IsAlignedBodyAndArms(skeleton))
                    {
                        parte2 = true;
                        parte1 = false;
                        repeticiones = repeticiones_totales; 
                        Repeticiones.Text = repeticiones.ToString(); // Mostramos las repeticiones actuales a hacer por pantalla
                        Instruccion.Text = "Suba los brazos";

                        // En esta parte se calcula la longitud de los brazos del usuario 
                        calcular_longitud_brazos(skeleton);
                    }
                }
                    // Segunda parte: levantar mancuernas con ambos brazos en el eje XY
                else if (parte2)
                {
                    if (pintar) // Si cierto pintar los puntos de referencia superiores
                    {
                        //Actualizamos los puntos de referencia 
                        obj_der_sup = puntoAux4;
                        obj_izq_sup = puntoAux5;
                        obj_der_sup.X = puntoAux4.X + longitud_brazos;
                        obj_izq_sup.X = puntoAux5.X - longitud_brazos;

                        //Pintamos los puntos de referencia
                        pintar_punto(this.trackedJointBrush, obj_der_sup, drawingContext);
                        pintar_punto(this.trackedJointBrush, obj_izq_sup, drawingContext);
                    }
                    else
                    {
                        //Actualizamos los puntos de referencia 
                        obj_der_inf = puntoAux4;
                        obj_izq_inf = puntoAux5;
                        obj_der_inf.Y = puntoAux4.Y + longitud_brazos;
                        obj_izq_inf.Y = puntoAux5.Y + longitud_brazos;

                        //Pintamos los puntos de referencia
                        pintar_punto(this.trackedJointBrush, obj_der_inf, drawingContext);
                        pintar_punto(this.trackedJointBrush, obj_izq_inf, drawingContext);
                    }
                    switch (ejerc1_parte2(skeleton))
                    {
                        case 1: if (!escrito) // Para controlar que no se actualiza más de una vez $repeticiones
                            { 
                                repeticiones--;
                                Repeticiones.Text = repeticiones.ToString(); // Mostramos las repeticiones actuales a hacer por pantalla
                                escrito = true; // Una vez actualizada, se bloquea su acceso
                                bajar_brazos = false; // Ahora deben subirse los brazos
                                Instruccion.Text = "Suba los brazos";
                            }
                            break;
                        case -1: Instruccion.Text = "Error";
                            n_error++;
                            break;
                    }
                    // Si se llega a este punto cambiamos a la siguiente parte del ejercicio
                    if (repeticiones == 0)
                    {
                        parte2 = false;
                        parte3 = true;
                        repeticiones = repeticiones_totales;
                        Repeticiones.Text = repeticiones.ToString(); // Mostramos las repeticiones actuales a hacer por pantalla
                    }
                }
                else if (parte3)
                {
                    Instruccion.Text = "Fin del Itinerario 1. Perfecto!";
                    ejercicio1 = false;
                    Itinerario1.Visibility = Visibility.Visible;
                    Itinerario2.Visibility = Visibility.Visible;
                    Itinerario3.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// En esta parte se calcula la longitud de los brazos del usuario y se actualizan los puntos de referencia objetivo
        /// </summary>
        /// <param name="skeleton"></param>
        private void calcular_longitud_brazos(Skeleton skeleton){

            Point puntoAux1 = this.SkeletonPointToScreen(skeleton.Joints[JointType.ShoulderRight].Position);
            Point puntoAux2 = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position);
            Point puntoAux3 = this.SkeletonPointToScreen(skeleton.Joints[JointType.ShoulderLeft].Position);
            Point puntoAux6 = this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position);

            longitud_brazos = Math.Abs(puntoAux2.Y - puntoAux1.Y);

            //Se asignan los puntos de referencia a los que deberá llegar el usuario para ir avanzando en el ejercicio
            obj_der_sup = puntoAux1;
            obj_izq_sup = puntoAux3;
            obj_der_sup.X = obj_der_sup.X + longitud_brazos;
            obj_izq_sup.X = obj_izq_sup.X - longitud_brazos;
            obj_der_inf = puntoAux2;
            obj_izq_inf = puntoAux6;
        }

        /// <summary>
        /// Pinta un punto de color $brush en la posición 2D $point en el contexto $drawingContext
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="point"></param>
        /// <param name="drawingContext"></param>
        private void pintar_punto(Brush brush, Point point, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(brush, null, point, JointThickness, JointThickness);
        }

    }
}