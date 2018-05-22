using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;


namespace AUModule
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Used to display 1,000 points on screen.
        private List<Ellipse> _points = new List<Ellipse>();

        private List<double> _columnsCSV;
        private List<List<double>> _rowsCSV;
        private List<String> _timeStamps;
        private List<String> _annotations;


        private List<double> _rotationW;
        private List<double> _rotationX;
        private List<double> _rotationY;
        private List<double> _rotationZ;

        

        private Boolean _printDebugs = false;

        private Boolean _isLoad = false;
        

        private List<String> _outputList;

        private List<double> _restRow;
        private int _restThreshold = 10;

        private List<List<double>> _AUs;
        private List<string> _nameAU;
        private List<string> _numAU;

        public MainWindow()
        {
            this.InitializeComponent();


            //UpdateFacePoints();
        }


      


        private void Dir_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                Box_File.Text = (openFileDialog.FileName);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            _rotationW = new List<double>();
            _rotationX = new List<double>();
            _rotationY = new List<double>();
            _rotationZ = new List<double>();
            _timeStamps = new List<String>();
            _annotations = new List<String>();

            using (var reader = new StreamReader(Box_File.Text))
            {
                reader.ReadLine();
                reader.ReadLine();

                _rowsCSV = new List<List<double>>();

                while (!reader.EndOfStream)
                {
                    _columnsCSV = new List<double>();


                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    _timeStamps.Add(values[0]);
                    _annotations.Add(values[1]);
                    _rotationW.Add(float.Parse(values[2]));
                    _rotationX.Add(float.Parse(values[3]));
                    _rotationY.Add(float.Parse(values[4]));
                    _rotationZ.Add(float.Parse(values[5]));

                    for (int i = 6; i < values.Length; i++)
                    {
                        if (values[i] != null && values[i] != "")
                            _columnsCSV.Add(Double.Parse(values[i]));
                    }

                    _rowsCSV.Add(_columnsCSV);
                }
            }

               

            string[] text = Box_File.Text.Split('\\');
            lbl_Loaded.Content = "LOADED: "+ text[text.Length-1];
            _isLoad = true;

            InitRest();
            RecordAUs();
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoad)
                return;
            CSVWriter csvWriter = new CSVWriter();
            _outputList = new List<string>();

            string[] text = Box_File.Text.Split('\\');

            Console.WriteLine(Box_File.Text + " " + text[text.Length - 1].Split('.')[0]);
            
            csvWriter.Start(Box_File.Text,text[text.Length - 1].Split('.')[0]);

            for (int rowIndex = 0; rowIndex < _rowsCSV.Count(); rowIndex++)
            {
                Quaternion v4 = new Quaternion(_rotationX[rowIndex], _rotationY[rowIndex], _rotationZ[rowIndex], _rotationW[rowIndex]);
                Vector3D head = new Vector3D(_rowsCSV[rowIndex][0], _rowsCSV[rowIndex][1], _rowsCSV[rowIndex][2]);

                List<double> auRow = new List<double>();
                for (int i = 0; i < _nameAU.Count; i++) {
                    if (rowIndex < _AUs[i].Count)
                        auRow.Add(_AUs[i][rowIndex]);
                    else
                        auRow.Add(-99999);
                
                }
                    
                _outputList.Add(csvWriter.UpdatePoints(auRow, _nameAU, _numAU,_annotations[rowIndex], v4, head, _timeStamps[rowIndex]));
            }

            if(csvWriter.Stop(_outputList))
            MessageBox.Show("Complete: File Saved");
            else
                MessageBox.Show("Save Failed");
        }

        private void InitRest()
        {
            int rowCount = _rowsCSV.Count;
            int indexOfRest = _annotations.FindIndex(x => x.Equals("rest", StringComparison.OrdinalIgnoreCase));


            if (indexOfRest == -1)
            {
                indexOfRest = 0;
            }

            Console.WriteLine("Index: " + indexOfRest);


            //Gets some Rest

            int numCols = _rowsCSV[0].Count; //4 is for rotation vectors

            List<double> temp = new List<double>();

            for(int i = 0; i < 4 + numCols; i++)
            {
                temp.Add(0);
            }
            //List<double> temp = new List<double>;
            for (int i = 0; i< _restThreshold; i++)
            {
                temp[0] += (_rotationW[i + indexOfRest] / _restThreshold);
                temp[1] += (_rotationX[i + indexOfRest] / _restThreshold);
                temp[2] += (_rotationY[i + indexOfRest] / _restThreshold);
                temp[3] += (_rotationZ[i + indexOfRest] / _restThreshold);

                for (int j = 0; j <numCols; j++)
                {
                    temp[j+4] += (_rowsCSV[i + indexOfRest][j]/ _restThreshold);
                }
            }

            _restRow = temp;
     
        }

        private void RecordAUs()
        {
            _nameAU = new List<string>();
            _numAU = new List<string>();
            _AUs = new List<List<double>>();


            //LeftEyeInner = 43 44 45
            //LeftEyebrowInner = 52 53 54
            //RightEyebrowInner = 82 83 84
            //RightEyeInner = 85 86 87
            //MouthUpperlipMidbottom = 94 95 96
            //MouthLowerlipMidtop = 13 14 15
            //LeftEyeMidTop = 49 50 51
            //RightEyeMidTop = 70 71 72
            //LeftEyeMidBottom = 97 98 99
            //RightEyeMidBottom = 100 101 102
            //MouthLeftCorner = 31 32 33
            //MouthRightCorner = 67 68 69



            /*
             * Initialize rest points
             */

            Vector3D restChinCenter = new Vector3D(_restRow[7], _restRow[8], _restRow[9]);
            Vector3D restMouthLowerlipMidBottom = new Vector3D(_restRow[10], _restRow[11], _restRow[12]);
            Vector3D restMouthLowerlipMidTop = new Vector3D(_restRow[13], _restRow[14], _restRow[15]);
            Vector3D restMouthUpperlipMidTop = new Vector3D(_restRow[22], _restRow[23], _restRow[24]);
            Vector3D restLeftEyeBrowOuter = new Vector3D(_restRow[34], _restRow[35], _restRow[36]);
            Vector3D restLeftEyeInner = new Vector3D(_restRow[43], _restRow[44], _restRow[45]);
            Vector3D restLeftEyeMidTop = new Vector3D(_restRow[49], _restRow[50], _restRow[51]);
            Vector3D restLeftEyeBrowInner = new Vector3D(_restRow[52], _restRow[53], _restRow[54]);
            Vector3D restLeftEyeOuterCorner = new Vector3D(_restRow[61], _restRow[62], _restRow[63]);
            Vector3D restRightEyeMidTop = new Vector3D(_restRow[70], _restRow[71], _restRow[72]);
            Vector3D restRightEyeBrowOuter = new Vector3D(_restRow[73], _restRow[74], _restRow[75]);
            Vector3D restRightEyeBrowInner = new Vector3D(_restRow[82], _restRow[83], _restRow[84]);
            Vector3D restRightEyeInner = new Vector3D(_restRow[85], _restRow[86], _restRow[87]);
            Vector3D restMouthUpperlipMidBottom = new Vector3D(_restRow[94], _restRow[95], _restRow[96]);
            Vector3D restLeftEyeMidBottom = new Vector3D(_restRow[97], _restRow[98], _restRow[99]);
            Vector3D restRightEyeMidBottom = new Vector3D(_restRow[100], _restRow[101], _restRow[102]);
            Vector3D restRightEyeOuterCorner = new Vector3D(_restRow[103], _restRow[104], _restRow[105]);
            Vector3D restMouthLeftCorner = new Vector3D(_restRow[31], _restRow[32], _restRow[33]);
            Vector3D restMouthRightCorner = new Vector3D(_restRow[67], _restRow[68], _restRow[69]);







            /*
             *  AU1 Inner brow raiser
             */
            {
                _nameAU.Add("InnerBrowRiser");
                _numAU.Add("AU1");
                List<double> au1 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeInner, restLeftEyeBrowInner);
                double restRightDistance = Distance3D(restRightEyeInner, restRightEyeBrowInner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeInner = new Vector3D(currRow[43 - 4], currRow[44 - 4], currRow[45 - 4]);
                    Vector3D LeftEyeBrowInner = new Vector3D(currRow[52 - 4], currRow[53 - 4], currRow[54 - 4]);
                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83 - 4], currRow[84 - 4]);
                    Vector3D RightEyeInner = new Vector3D(currRow[85 - 4], currRow[86 - 4], currRow[87 - 4]);

                    double LeftDistance = Distance3D(LeftEyeInner, LeftEyeBrowInner);
                    double RightDistance = Distance3D(RightEyeInner, RightEyeBrowInner);

                    double output = ((LeftDistance - restLeftDistance) + (RightDistance - restRightDistance)) / 2;



                    au1.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au1);



            }

            /*
             * AU2 Outer Brow Raiser //VICNENT yes vicnent
             */
            {
                _nameAU.Add("outerBrowRiser");
                _numAU.Add("AU2");
                List<double> au2 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeOuterCorner, restLeftEyeBrowOuter);
                double restRightDistance = Distance3D(restRightEyeOuterCorner, restRightEyeBrowOuter);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeOuterCorner = new Vector3D(currRow[61 - 4], currRow[62 - 4], currRow[63 - 4]);
                    Vector3D LeftEyeBrowOuter = new Vector3D(currRow[34 - 4], currRow[35 - 4], currRow[36 - 4]);
                    Vector3D RightEyeBrowOuter = new Vector3D(currRow[73 - 4], currRow[74 - 4], currRow[75 - 4]);
                    Vector3D RightEyeOuterCorner = new Vector3D(currRow[103 - 4], currRow[104 - 4], currRow[105 - 4]);

                    double LeftDistance = Distance3D(LeftEyeOuterCorner, LeftEyeBrowOuter);
                    double RightDistance = Distance3D(RightEyeOuterCorner, RightEyeBrowOuter);

                    double output = ((LeftDistance - restLeftDistance) + (RightDistance - restRightDistance)) / 2;



                    au2.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au2);



            }

            /*
             *  AU4 Brow Lowerer
             */
            {
                _nameAU.Add("BrowLowerer");
                _numAU.Add("AU4");
                List<double> au4 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeInner, restLeftEyeBrowInner);
                double restRightDistance = Distance3D(restRightEyeInner, restRightEyeBrowInner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeInner = new Vector3D(currRow[43 - 4], currRow[44 - 4], currRow[45 - 4]);
                    Vector3D LeftEyeBrowInner = new Vector3D(currRow[52 - 4], currRow[53 - 4], currRow[54 - 4]);
                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83 - 4], currRow[84 - 4]);
                    Vector3D RightEyeInner = new Vector3D(currRow[85 - 4], currRow[86 - 4], currRow[87 - 4]);

                    double LeftDistance = Distance3D(LeftEyeInner, LeftEyeBrowInner);
                    double RightDistance = Distance3D(RightEyeInner, RightEyeBrowInner);

                    double output = ((LeftDistance - restLeftDistance) + (RightDistance - restRightDistance)) / 2;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au4.Add(output < 0 ? -output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au4);



            }

            /*AU5 Upper Lid Raiser //Ralph
            *
            * LeftEyeMidTop = 49 50 51
            * RightEyeMidTop = 70 71 72
            * LeftEyeMidBottom = 97 98 99
            * RightEyeMidBottom = 100 101 102
            */
            {
                _nameAU.Add("Upper Lid Raiser");
                _numAU.Add("AU5");
                List<double> au5 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeMidTop, restLeftEyeMidBottom);
                double restRightDistance = Distance3D(restRightEyeMidTop, restRightEyeMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeMidTop = new Vector3D(currRow[49 - 4], currRow[50 - 4], currRow[51 - 4]);
                    Vector3D LeftEyeMidBottom = new Vector3D(currRow[97 - 4], currRow[98 - 4], currRow[99 - 4]);
                    Vector3D RightEyeMidTop = new Vector3D(currRow[70 - 4], currRow[71 - 4], currRow[72 - 4]);
                    Vector3D RightEyeMidBottom = new Vector3D(currRow[100 - 4], currRow[101 - 4], currRow[102 - 4]);

                    double LeftDistance = Distance3D(LeftEyeMidTop, LeftEyeMidBottom);
                    double RightDistance = Distance3D(RightEyeMidTop, RightEyeMidBottom);

                    double output = ((LeftDistance - restLeftDistance) + (RightDistance - restRightDistance)) / 2;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au5.Add(output > 0 ? output : 0);
                    //au5.Add(output);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au5);
            }

            /*AU6 Cheek Raiser //Ralph
            *
            * 
            */
            /*
            {
                _nameAU.Add("Upper Lid Raiser");
                _numAU.Add("AU5");
                List<double> au5 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeMidTop, restLeftEyeMidBottom);
                double restRightDistance = Distance3D(restRightEyeMidTop, restRightEyeMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeMidTop = new Vector3D(currRow[49 - 4], currRow[50 - 4], currRow[51 - 4]);
                    Vector3D LeftEyeMidBottom = new Vector3D(currRow[97 - 4], currRow[98 - 4], currRow[99 - 4]);
                    Vector3D RightEyeMidTop = new Vector3D(currRow[70 - 4], currRow[71 - 4], currRow[72 - 4]);
                    Vector3D RightEyeMidBottom = new Vector3D(currRow[100 - 4], currRow[101 - 4], currRow[102 - 4]);

                    double LeftDistance = Distance3D(LeftEyeMidTop, LeftEyeMidBottom);
                    double RightDistance = Distance3D(RightEyeMidTop, RightEyeMidBottom);

                    double output = ((LeftDistance - restLeftDistance) + (RightDistance - restRightDistance)) / 2;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au5.Add(output > 0 ? output : 0);
                    //au5.Add(output);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au5);
            }
            */

            /*
            * AU12 Lip Corner Puller //Ralph
            * MouthLeftCorner = 31 32 33
            * MouthRightCorner = 67 68 69
            */
            {

                _nameAU.Add("Lip Corner Puller");
                _numAU.Add("AU12");
                List<double> au12 = new List<double>();

                double avgRestLipCornerHeight = restMouthLeftCorner.Y + restMouthRightCorner.Y / 2;

                double restDistance = restMouthUpperlipMidBottom.Y - avgRestLipCornerHeight ;

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);
                    Vector3D MouthUpperLipMidBottom = new Vector3D(currRow[94 - 4], currRow[95 - 4], currRow[96 - 4]);

                    double avgLipCornerHeight = MouthLeftCorner.Y + MouthRightCorner.Y / 2;

                    double distance = MouthUpperLipMidBottom.Y - avgLipCornerHeight;

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = restDistance - distance;

                    au12.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au12);
            }

            /*
            * AU14 Dimpler //Ralph
            * MouthLeftCorner = 31 32 33
            * MouthRightCorner = 67 68 69
            */
            
            {

                _nameAU.Add("Dimpler");
                _numAU.Add("AU14");
                List<double> au12 = new List<double>();

                double restDistance = Distance3D(restMouthLeftCorner, restMouthRightCorner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);

                    double distance = Distance3D(MouthLeftCorner, MouthRightCorner);

                    double output = distance - restDistance;

                    au12.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);
                }

                _AUs.Add(au12);
            }
            

            //TODO
            /*
            * AU15 Lip Corner Depressor //Ralph   I HAVE A PROBLEM HERE
            * MouthLeftCorner = 31 32 33
            * MouthRightCorner = 67 68 69
            */
            {
                
                _nameAU.Add("Lip Corner Depressor");
                _numAU.Add("AU15");
                List<double> au15 = new List<double>();

                double avgRestLipCornerHeight = restMouthLeftCorner.Y + restMouthRightCorner.Y / 2;

                double restDistance = restMouthLowerlipMidTop.Y - avgRestLipCornerHeight;

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);
                    Vector3D MouthLowerlipMidtop = new Vector3D(currRow[13 - 4], currRow[14 - 4], currRow[15 - 4]);

                    double avgLipCornerHeight = MouthLeftCorner.Y + MouthRightCorner.Y / 2;

                    double distance = MouthLowerlipMidtop.Y - avgLipCornerHeight;

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = restDistance - distance;

                    au15.Add(output < 0 ? -output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au15);
            }


            /*
             * AU17 Chin Raiser //Vincet
             */
            {
                _nameAU.Add("Chin Raiser");
                _numAU.Add("AU17");
                List<double> au17 = new List<double>();

                double restDistance = Distance3D(restChinCenter, restMouthLowerlipMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D chinCenter = new Vector3D(currRow[7 - 4], currRow[8 - 4], currRow[9 - 4]);
                    Vector3D MouthLowerMidBottom = new Vector3D(currRow[10 - 4], currRow[11 - 4], currRow[12 - 4]);

                    double auDistance = Distance3D(chinCenter, MouthLowerMidBottom);

                    double output = restDistance - auDistance;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au17.Add(output > 0 ? output : 0);
                    //au5.Add(output);

                    //Console.WriteLine(output);

                }


                _AUs.Add(au17);
            }

            /*
             *  AU18 Lip Puckerer
             *  MouthLeftCorner = 31 32 33
            *   MouthRightCorner = 67 68 69
             */
            {
                _nameAU.Add("LipPuckerer");
                _numAU.Add("AU18");
                List<double> au18 = new List<double>();

                double avgRestZDistance = (restMouthLowerlipMidTop.Z + restMouthLowerlipMidBottom.Z + restMouthUpperlipMidBottom.Z +restMouthUpperlipMidTop.Z)/4;
                double restMouthDistance = Distance3D(restMouthLeftCorner, restMouthRightCorner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    
                    
                    Vector3D MouthLowerlipMidBottom = new Vector3D(currRow[10 - 4], currRow[11 - 4], currRow[12 - 4]);
                    Vector3D MouthLowerlipMidTop = new Vector3D(currRow[13 - 4], currRow[14 - 4], currRow[15 - 4]);
                    Vector3D MouthUpperlipMidTop = new Vector3D(currRow[22 - 4], currRow[23 - 4], currRow[24 - 4]);
                    Vector3D MouthUpperlipMidBottom = new Vector3D(currRow[94 - 4], currRow[95 - 4], currRow[96 - 4]);

                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);

                    double MouthDistance = Distance3D(MouthLeftCorner, MouthRightCorner);

                    double avgZDistance = (MouthLowerlipMidTop.Z + MouthLowerlipMidBottom.Z + MouthUpperlipMidBottom.Z + MouthUpperlipMidTop.Z) / 4;

                    double outputDistance = (restMouthDistance - MouthDistance) > 0 ? (restMouthDistance - MouthDistance) : 0;

                    double outputAvgZDistance = (avgRestZDistance - avgZDistance) > 0 ? (avgRestZDistance - avgZDistance) : 0;

                    double output = (outputAvgZDistance + outputDistance)/2;
                    au18.Add(output);
                    //Console.WriteLine(output);

                }


                _AUs.Add(au18);


            }

            /*
             * AU23 Lip Tightener
             */
            {
                _nameAU.Add("LipTightener");
                _numAU.Add("AU23");
                List<double> au23 = new List<double>();
                
                double restMouthDistance = Distance3D(restMouthLeftCorner, restMouthRightCorner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];



                    
                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);

                    double MouthDistance = Distance3D(MouthLeftCorner, MouthRightCorner);
                    

                    double outputDistance = (restMouthDistance - MouthDistance) > 0 ? (restMouthDistance - MouthDistance) : 0;


                    double output = outputDistance;
                    au18.Add(output);
                    //Console.WriteLine(output);

                }

                _AUs.Add(au23);
            }


            /*
                * AU27 Mouth Stretch //Ralph
                */
            {
                _nameAU.Add("MouthStretch");
                _numAU.Add("AU27");
                List<double> au27 = new List<double>();

                double restDistance = Distance3D(restMouthUpperlipMidBottom, restMouthLowerlipMidTop);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];


                    Vector3D MouthUpperlipMidbottom = new Vector3D(currRow[94 - 4], currRow[95 - 4], currRow[96 - 4]);
                    Vector3D MouthLowerlipMidtop = new Vector3D(currRow[13 - 4], currRow[14 - 4], currRow[15 - 4]);

                    double MouthStretchDistance = Distance3D(MouthUpperlipMidbottom, MouthLowerlipMidtop);

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = Math.Abs(restDistance - MouthStretchDistance);

                    au27.Add(output);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au27);

            }

            /*
             * AU41-44 Eyes Closed //Vincent
             */
            {
                _nameAU.Add("EyesClosed");
                _numAU.Add("AU41-44");
                List<double> au44 = new List<double>();


                double restLeftDistance = Distance3D(restLeftEyeMidTop, restLeftEyeMidBottom);
                double restRightDistance = Distance3D(restRightEyeMidTop, restRightEyeMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeMidTop = new Vector3D(currRow[49 - 4], currRow[50 - 4], currRow[51 - 4]);
                    Vector3D LeftEyeMidBottom = new Vector3D(currRow[97 - 4], currRow[98 - 4], currRow[99 - 4]);
                    Vector3D RightEyeMidTop = new Vector3D(currRow[70 - 4], currRow[71 - 4], currRow[72 - 4]);
                    Vector3D RightEyeMidBottom = new Vector3D(currRow[100 - 4], currRow[101 - 4], currRow[102 - 4]);

                    double LeftDistance = Distance3D(LeftEyeMidTop, LeftEyeMidBottom);
                    double RightDistance = Distance3D(RightEyeMidTop, RightEyeMidBottom);

                    double output = (((restLeftDistance - LeftDistance) > 0 ? (restLeftDistance - LeftDistance) : 0 )+ ((restRightDistance - RightDistance) >0 ? (restRightDistance - RightDistance) : 0)) / 2;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au44.Add(output);
                }

                _AUs.Add(au44);

            }
        }

        private double Distance3D(Vector3D A,Vector3D B)
        {
            double deltaX = A.X - B.X;
            double deltaY = A.Y - B.Y;
            double deltaZ = A.Z - B.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

    }
}