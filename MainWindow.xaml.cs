//------------------------------------------------------------------------------
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
        private readonly Pen penCorrecto = new Pen(Brushes.Green, 6);
        private readonly Brush puntoVerde = Brushes.GreenYellow;

        private readonly Pen penTranscurso = new Pen(Brushes.Yellow, 6);
        private readonly Brush puntoAmarillo = Brushes.Yellow;

        private readonly Pen penInicio = new Pen(Brushes.Cyan, 6);
        private readonly Brush puntoCyan = Brushes.Cyan;


        private readonly Pen penError = new Pen(Brushes.Red, 6);
        private readonly Brush puntoRojo = Brushes.Red;


        private readonly Pen drawPen = new Pen(Brushes.Blue, 6);

        //flags
        private bool mov7 = false;
        private float mano_derecha;
        private float mano_izquierda;
        private bool escrito = false; //Mientras sea false se puede cambiar un contador

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
        private int repeticiones = 3;
        private int rep = 0;

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
            error = Regulador.Value;
            valor_error.Content = error.ToString();

            /*
            Pen drawPenBrazo;
            float distancia = 0.15F; //distancia que queremos que cubra la mano izquierda
            //Primero habrá que completar la posición de inicio (brazos en cruz) antes de continuar
            if (inicio) {
                drawPenBrazo = LeftArmInit(skeleton);
            }else if(transcurso){
                drawPenBrazo = LeftArmPosition(skeleton, distancia);
            }else
                drawPenBrazo = penError;*/
            
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight, drawPen);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft, drawPen);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight, drawPen);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft, drawPen);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight, drawPen);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight, drawPen);

            if (ejercicio1) {
                Descrip_mov.Text = "Póngase en posición relajada y los brazos pegados al cuerpo. A continuación suba los brazos hasta \nponerlos en cruz. Repita ";
                Descrip_mov.Foreground = Brushes.DarkBlue;
                Repeticiones.Text = rep.ToString();                
                //Primera parte: comprobamos que estamos en posición de inicio, relajada
                if (parte1)
                {                    
                    if (IsAlignedBodyAndArms(skeleton)){
                        parte2 = true;
                        parte1 = false;
                        rep = repeticiones;
                        Repeticiones.Text = rep.ToString();
                        Instruccion.Text = "Suba los brazos";
                    }                    
                }
                else if (parte2) {                    
                    switch (movimiento7(skeleton)) {
                        case 1: if (!escrito) {
                                    rep--;
                                    Repeticiones.Text = rep.ToString();
                                    escrito = true;
                                    mov7 = false;
                                    Instruccion.Text = "Suba los brazos";
                                }                                
                                break;
                        case -1: Instruccion.Text = "Error"; 
                                n_error++;
                                break;
                    }
                    if (rep == 0) {
                        parte2 = false;
                        parte3 = true;
                        rep = repeticiones;
                        Repeticiones.Text = rep.ToString();
                    }                
                }
            }

            //Aquí se añadirían más ejercicios

            // Render Joints
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
            }
        }
        /// <summary>
        /// Comprueba si la posición de inicio se realiza correctamente
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        /*Pen LeftArmInit(Skeleton skeleton)
        {
            Pen drawPen;
            if (//Comprueba que el brazo izquierdo esté posicionado correctamente
                (skeleton.Joints[JointType.HandLeft].Position.Z + 0.15 > skeleton.Joints[JointType.ShoulderLeft].Position.Z) &&
                (skeleton.Joints[JointType.HandLeft].Position.Z - 0.15 < skeleton.Joints[JointType.ShoulderLeft].Position.Z) &&
                (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X) &&
                (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.WristLeft].Position.X) &&
                //Comprueba que el brazo derecho esté posicionado correctamente
                (skeleton.Joints[JointType.HandRight].Position.Z + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.Z - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.WristRight].Position.X) &&
                //Comprueba que ambas manos estén al nivel de los hombros
                (skeleton.Joints[JointType.HandRight].Position.Y + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandRight].Position.Y - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Y)
                )
            {
                drawPen = penInicio;
                acumulador++; 
                //Comprobamos que la posición de inicio se ha mantenido durante un tiempo
                if (acumulador > 100)
                {
                    //Posición de inicio alcanzada correctamente
                    drawPen = penTranscurso; 
                    inicio = false;
                    transcurso = true;
                    //Guardamos la posición de la mano izquierda como referencia para realizar el Movimiento 32
                    referencia = skeleton.Joints[JointType.HandLeft].Position.X; 
                }
            }
            else drawPen = penError;

            return drawPen;
        }*/

        /// <summary>
        /// Comprueba el movimiento de la mano izquierda en el eje X se realiza correctamente
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="distancia"></param>
        /// <returns></returns>
        /*Pen LeftArmPosition(Skeleton skeleton, float distancia)
        {
            Pen drawPen;
            if(completado)
                return drawPen = penCorrecto;

            if (//Comprobamos que se realiza el Movimiento 32
                (skeleton.Joints[JointType.HandLeft].Position.X > referencia + distancia) &&
                (skeleton.Joints[JointType.HandLeft].Position.X < referencia + distancia + 0.10) &&

                //Comprueba que el brazo derecho esté posicionado correctamente
                (skeleton.Joints[JointType.HandRight].Position.Z + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.Z - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Z) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X) &&
                (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.WristRight].Position.X) &&
                //Comprueba que ambas manos estén al nivel de los hombros
                (skeleton.Joints[JointType.HandRight].Position.Y + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandRight].Position.Y - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y + 0.15 > skeleton.Joints[JointType.ShoulderRight].Position.Y) &&
                (skeleton.Joints[JointType.HandLeft].Position.Y - 0.15 < skeleton.Joints[JointType.ShoulderRight].Position.Y)
                )
            {
                drawPen = penCorrecto; //Movimiento realizado correctamente
                completado = true;
            }
            else drawPen = penTranscurso;

            return drawPen;
        }*/

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

        // boolean method that return true if body is completely aligned and arms are in a relaxed position
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
                    mano_derecha = received.Joints[JointType.HandRight].Position.Y;
                    mano_izquierda = received.Joints[JointType.HandLeft].Position.Y;
                    return true;
                }
                else return false;
            }
            else return false;

        }

        /// <summary>
        /// Handler for click event from "Reset Reconstruction" button
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void Itinerario1_Click(object sender, RoutedEventArgs e) {
            if (null == this.sensor)
            {
                return;
            }
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
        /// Calcula la diferencia entre dos valores.
        /// </summary>
        private float diff(float v1, float v2) { return Math.Abs(v1 - v2); }

        /// <summary>
        /// Movimiento basado en la posicion 7 y 1
        /// </summary>
        /// <param name="skel"></param>
        /// <returns></returns>
        private int movimiento7(Skeleton skeleton) {
            double p_error = error / 100;
            Titulo.Text =  p_error.ToString();


            /*if ((mano_derecha * (1.20) > skel.Joints[JointType.HandRight].Position.Y) && !mov7)
                return -1;
            if ((mano_izquierda * (1.20) > skel.Joints[JointType.HandLeft].Position.Y) && !mov7)
                return -1;*/

            //Comprueba que las manos están a la altura de los hombros
            if (//Comprobamos que se realiza el Movimiento 32
                //(skeleton.Joints[JointType.HandLeft].Position.X > referencia + distancia) &&
                //(skeleton.Joints[JointType.HandLeft].Position.X < referencia + distancia + 0.10) &&

                //Comprobamos que los brazos están en cruz
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
                !mov7
                )
            {
                escrito = false;
                mov7 = true;
                Instruccion.Text = "Ahora baje los brazos";
            }
            //mano_derecha = skeleton.Joints[JointType.HandRight].Position.Y;
            //mano_izquierda = skeleton.Joints[JointType.HandLeft].Position.Y;
            if (mov7 && IsAlignedBodyAndArms(skeleton))
                return 1;
            else              
                return 0;
        }

    }
}