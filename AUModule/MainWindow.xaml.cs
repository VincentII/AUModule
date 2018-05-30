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

        private String[] _listName;
        

        private List<String> _outputList;



        private List<double> _restRow;
        private int _restThreshold = 10;

        private List<List<double>> _AUs;
        private List<string> _nameAU;
        private List<string> _numAU;

        public MainWindow()
        {
            this.InitializeComponent();

            
        }


      


        private void Dir_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*";

            openFileDialog.Multiselect=true;

            

            if (openFileDialog.ShowDialog() == true)
                Box_File.Text = (openFileDialog.FileName);

            _listName = openFileDialog.FileNames;

        }


        
        private void PreLoad(String file)
        {
            _rotationW = new List<double>();
            _rotationX = new List<double>();
            _rotationY = new List<double>();
            _rotationZ = new List<double>();
            _timeStamps = new List<String>();
            _annotations = new List<String>();

            using (var reader = new StreamReader(file))
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
            lbl_Loaded.Content = "LOADED: " + text[text.Length - 1];
            
            InitRest();
            RecordAUs();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Boolean output = false;
            List<int> failed = new List<int>();

            if (_listName.Count() >= 1)
                output = true;

            MessageBox.Show("LOL"+ output.ToString());

            for (int i = 0; i < _listName.Count(); i++)
            {
                PreLoad(_listName[i]);
                Boolean temp = WriteFile(_listName[i]);

                if (output && temp)
                    output = true;
                else { 
                    output = false;
                    failed.Add(i);
                }
            }

            if (output)
                MessageBox.Show("Complete: Files Saved");
            else
            {
                MessageBox.Show("File/s Failed");
                for (int i = 0; i < failed.Count(); i++)
                    MessageBox.Show("A file Failed: "+failed[i]);
            }
                
        }

        private Boolean WriteFile(String file)
        {
            CSVWriter csvWriter = new CSVWriter();
            _outputList = new List<string>();

            string[] text = file.Split('\\');

            Console.WriteLine(file + " " + text[text.Length - 1].Split('.')[0]);

            csvWriter.Start(file, text[text.Length - 1].Split('.')[0]);

            for (int rowIndex = 0; rowIndex < _rowsCSV.Count(); rowIndex++)
            {
                System.Windows.Media.Media3D.Quaternion v4 = new System.Windows.Media.Media3D.Quaternion(_rotationX[rowIndex], _rotationY[rowIndex], _rotationZ[rowIndex], _rotationW[rowIndex]);
                Vector3D head = new Vector3D(_rowsCSV[rowIndex][0], _rowsCSV[rowIndex][1], _rowsCSV[rowIndex][2]);

                List<double> auRow = new List<double>();
                for (int i = 0; i < _nameAU.Count; i++)
                {
                    if (rowIndex < _AUs[i].Count)
                        auRow.Add(_AUs[i][rowIndex]);
                    else
                        auRow.Add(-99999);

                }

                _outputList.Add(csvWriter.UpdatePoints(auRow, _nameAU, _numAU, _annotations[rowIndex], v4, head, _timeStamps[rowIndex]));
            }
            if (csvWriter.Stop(_outputList))
                return true;
            else
                return false;
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
            //LeftCheekBone = 58 59 60
            //RightCheekBone = 64 65 66
            //NoseBottomLeft = 40 41 42
            //NoseBottomRight = 79 80 81
            //MouthUpperLipMidTop = 22 23 24



            /*
             * Initialize rest points
             * 
             */

            Vector3D restHead = new Vector3D(_restRow[4], _restRow[5], _restRow[6]);
            Vector3D restChinCenter = new Vector3D(_restRow[7], _restRow[8], _restRow[9]);
            Vector3D restMouthLowerlipMidBottom = new Vector3D(_restRow[10], _restRow[11], _restRow[12]);
            Vector3D restMouthLowerlipMidTop = new Vector3D(_restRow[13], _restRow[14], _restRow[15]);
            Vector3D restNoseBottom = new Vector3D(_restRow[16], _restRow[17], _restRow[18]);
            Vector3D restMouthUpperlipMidTop = new Vector3D(_restRow[22], _restRow[23], _restRow[24]);
            Vector3D restNoseTop = new Vector3D(_restRow[25], _restRow[26], _restRow[27]);
            Vector3D restMouthLeftCorner = new Vector3D(_restRow[31], _restRow[32], _restRow[33]);
            Vector3D restLeftEyeBrowOuter = new Vector3D(_restRow[34], _restRow[35], _restRow[36]);
            Vector3D restNoseTopLeft = new Vector3D(_restRow[37], _restRow[38], _restRow[39]);
            Vector3D restNoseBottomLeft = new Vector3D(_restRow[40], _restRow[41], _restRow[42]);
            Vector3D restLeftEyeInner = new Vector3D(_restRow[43], _restRow[44], _restRow[45]);
            Vector3D restLeftEyeMidTop = new Vector3D(_restRow[49], _restRow[50], _restRow[51]);
            Vector3D restLeftEyeBrowInner = new Vector3D(_restRow[52], _restRow[53], _restRow[54]);
            Vector3D restLeftCheekBone = new Vector3D(_restRow[58], _restRow[59], _restRow[60]);
            Vector3D restLeftEyeOuterCorner = new Vector3D(_restRow[61], _restRow[62], _restRow[63]);
            Vector3D restRightCheekBone = new Vector3D(_restRow[64], _restRow[65], _restRow[66]);
            Vector3D restMouthRightCorner = new Vector3D(_restRow[67], _restRow[68], _restRow[69]);
            Vector3D restRightEyeMidTop = new Vector3D(_restRow[70], _restRow[71], _restRow[72]);
            Vector3D restRightEyeBrowOuter = new Vector3D(_restRow[73], _restRow[74], _restRow[75]);
            Vector3D restNoseTopRight = new Vector3D(_restRow[76], _restRow[77], _restRow[78]);
            Vector3D restNoseBottomRight = new Vector3D(_restRow[79], _restRow[80], _restRow[81]);
            Vector3D restRightEyeBrowInner = new Vector3D(_restRow[82], _restRow[83], _restRow[84]);
            Vector3D restRightEyeInner = new Vector3D(_restRow[85], _restRow[86], _restRow[87]);
            Vector3D restMouthUpperlipMidBottom = new Vector3D(_restRow[94], _restRow[95], _restRow[96]);
            Vector3D restLeftEyeMidBottom = new Vector3D(_restRow[97], _restRow[98], _restRow[99]);
            Vector3D restRightEyeMidBottom = new Vector3D(_restRow[100], _restRow[101], _restRow[102]);
            Vector3D restRightEyeOuterCorner = new Vector3D(_restRow[103], _restRow[104], _restRow[105]);
            
            
            
            
            
            





            /*
             *  AU1L Inner brow raiser L
             */
            {
                _nameAU.Add("Inner Brow Riser (Left)");
                _numAU.Add("AU1L");
                List<double> au1L = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeInner, restLeftEyeBrowInner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeInner = new Vector3D(currRow[43 - 4], currRow[44 - 4], currRow[45 - 4]);
                    Vector3D LeftEyeBrowInner = new Vector3D(currRow[52 - 4], currRow[53 - 4], currRow[54 - 4]);

                    double LeftDistance = Distance3D(LeftEyeInner, LeftEyeBrowInner);

                    double output = ((LeftDistance - restLeftDistance));



                    au1L.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au1L);



            }


            /*
             *  AU1R Inner brow raiser R
             */
            {
                _nameAU.Add("Inner Brow Riser (Right)");
                _numAU.Add("AU1R");
                List<double> au1R = new List<double>();
                
                double restRightDistance = Distance3D(restRightEyeInner, restRightEyeBrowInner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83 - 4], currRow[84 - 4]);
                    Vector3D RightEyeInner = new Vector3D(currRow[85 - 4], currRow[86 - 4], currRow[87 - 4]);
                    
                    double RightDistance = Distance3D(RightEyeInner, RightEyeBrowInner);

                    double output = ((RightDistance - restRightDistance));



                    au1R.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au1R);



            }

            /*
             * AU2L Outer Brow Raiser LEFT //VICNENT yes vicnent
             */
            {
                _nameAU.Add("outer Brow Riser (Left)");
                _numAU.Add("AU2L");
                List<double> au2L = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeOuterCorner, restLeftEyeBrowOuter);
                
                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeOuterCorner = new Vector3D(currRow[61 - 4], currRow[62 - 4], currRow[63 - 4]);
                    Vector3D LeftEyeBrowOuter = new Vector3D(currRow[34 - 4], currRow[35 - 4], currRow[36 - 4]);
                
                    double LeftDistance = Distance3D(LeftEyeOuterCorner, LeftEyeBrowOuter);
                
                    double output = ((LeftDistance - restLeftDistance));



                    au2L.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au2L);



            }


            /*
             * AU2R Outer Brow Raiser Right //VICNENT yes vicnent
             */
            {
                _nameAU.Add("Outer Brow Riser (Right)");
                _numAU.Add("AU2R");
                List<double> au2R = new List<double>();

                double restRightDistance = Distance3D(restRightEyeOuterCorner, restRightEyeBrowOuter);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D RightEyeBrowOuter = new Vector3D(currRow[73 - 4], currRow[74 - 4], currRow[75 - 4]);
                    Vector3D RightEyeOuterCorner = new Vector3D(currRow[103 - 4], currRow[104 - 4], currRow[105 - 4]);

                    double RightDistance = Distance3D(RightEyeOuterCorner, RightEyeBrowOuter);
                    double output = ((RightDistance - restRightDistance));



                    au2R.Add(output > 0 ? output : 0);



                }

                _AUs.Add(au2R);



            }

            /*
             *  AU4 Brow Lowerer Left
             */
            {
                _nameAU.Add("Brow Lowerer (Left)");
                _numAU.Add("AU4L");
                List<double> au4L = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeInner, restLeftEyeBrowInner);
                
                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeInner = new Vector3D(currRow[43 - 4], currRow[44 - 4], currRow[45 - 4]);
                    Vector3D LeftEyeBrowInner = new Vector3D(currRow[52 - 4], currRow[53 - 4], currRow[54 - 4]);
                
                    double LeftDistance = Distance3D(LeftEyeInner, LeftEyeBrowInner);
                
                    double output = ((LeftDistance - restLeftDistance));

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au4L.Add(output < 0 ? -output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au4L);



            }

            /*
            *  AU4 Brow Lowerer Right
            */
            {
                _nameAU.Add("BrowLowerer (Right)");
                _numAU.Add("AU4R");
                List<double> au4R = new List<double>();

                double restRightDistance = Distance3D(restRightEyeInner, restRightEyeBrowInner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83 - 4], currRow[84 - 4]);
                    Vector3D RightEyeInner = new Vector3D(currRow[85 - 4], currRow[86 - 4], currRow[87 - 4]);

                    double RightDistance = Distance3D(RightEyeInner, RightEyeBrowInner);

                    double output = ((RightDistance - restRightDistance));

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au4R.Add(output < 0 ? -output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au4R);



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
            * LeftCheekBone = 58 59 60
            * RightCheekBone = 64 65 66
            */

            {
                _nameAU.Add("Cheek Raiser");
                _numAU.Add("AU6");
                List<double> au6 = new List<double>();

                double avgRestHeight = restLeftCheekBone.Y + restRightCheekBone.Y / 2;

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftCheekBone = new Vector3D(currRow[58 - 4], currRow[59 - 4], currRow[60 - 4]);
                    Vector3D RightCheekBone = new Vector3D(currRow[64 - 4], currRow[65 - 4], currRow[66 - 4]);

                    double avgHeight = LeftCheekBone.Y + RightCheekBone.Y / 2;

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = avgHeight - avgRestHeight;

                    au6.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au6);
            }
            


            /*
             * AU9 Nose Wrinkler TODO Test //Vincent
             */
            {
                _nameAU.Add("Nose Wrinkler");
                _numAU.Add("AU9");
                List<double> au9 = new List<double>();

                double restLeftDistance = Distance3D(restNoseTop, restNoseTopLeft);
                double restRightDistance = Distance3D(restNoseTop, restNoseTopRight);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D NoseTop = new Vector3D(currRow[25 - 4], currRow[26 - 4], currRow[27 - 4]);
                    Vector3D NoseTopLeft = new Vector3D(currRow[37 - 4], currRow[38 - 4], currRow[39 - 4]);
                    Vector3D NoseTopRight = new Vector3D(currRow[76 - 4], currRow[77 - 4], currRow[78 - 4]);


                    double LeftDistance = Distance3D(NoseTop, NoseTopLeft);
                    double RightDistance = Distance3D(NoseTop, NoseTopRight);

                    double output = ((restLeftDistance - LeftDistance) + (restRightDistance - RightDistance)) / 2;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au9.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }


                _AUs.Add(au9);
            }

            /*
             *  AU10 Upper Lip Raiser //Vincent
             */

            {
                _nameAU.Add("Upper Lip Raiser");
                _numAU.Add("AU10");
                List<double> au10 = new List<double>();


                double restDistance = Distance3D(restNoseBottom, restMouthUpperlipMidTop);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D NoseBottom = new Vector3D(currRow[16 - 4], currRow[17 - 4], currRow[18 - 4]);
                    Vector3D MouthUpperLipMidBottom = new Vector3D(currRow[94 - 4], currRow[95 - 4], currRow[96 - 4]);



                    double distance = Distance3D(NoseBottom,MouthUpperLipMidBottom);

                    double output = restDistance - distance;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au10.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }


                _AUs.Add(au10);
            }

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
            * AU13 Cheek Puffer //Ralph
            *
            * // Vector3D restLeftEyeOuterCorner = new Vector3D(_restRow[61], _restRow[62], _restRow[63]);
            * // Vector3D restRightEyeOuterCorner = new Vector3D(_restRow[103], _restRow[104], _restRow[105]);
            * // Vector3D restLeftCheekBone = new Vector3D(_restRow[58], _restRow[59], _restRow[60]);
            * // Vector3D restRightCheekBone = new Vector3D(_restRow[64], _restRow[65], _restRow[66]);
            * //Vector3D restNoseBottomLeft = new Vector3D(_restRow[40], _restRow[41], _restRow[42]);
            * //Vector3D restNoseBottomRight = new Vector3D(_restRow[79], _restRow[80], _restRow[81]);
            */
            {

                _nameAU.Add("Cheek Puffer");
                _numAU.Add("AU13");
                List<double> au13 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeOuterCorner, restLeftCheekBone);
                double restRightDistance = Distance3D(restRightEyeOuterCorner, restRightCheekBone);
                double restDistance = restLeftDistance + restRightDistance / 2;

                double restNoseWideness = Distance3D(restNoseBottomLeft, restNoseBottomRight);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeOuterCorner = new Vector3D(currRow[61 - 4], currRow[62 - 4], currRow[63 - 4]);
                    Vector3D RightEyeOuterCorner = new Vector3D(currRow[103 - 4], currRow[104 - 4], currRow[105 - 4]);
                    Vector3D LeftCheekBone = new Vector3D(currRow[58 - 4], currRow[59 - 4], currRow[60 - 4]);
                    Vector3D RightCheekBone = new Vector3D(currRow[64 - 4], currRow[65 - 4], currRow[66 - 4]);

                    double leftDistance = Distance3D(LeftEyeOuterCorner, LeftCheekBone);
                    double rightDistance = Distance3D(RightEyeOuterCorner, RightCheekBone);

                    double distance = leftDistance + rightDistance / 2;
                    double output = restDistance - distance;


                    Vector3D NoseBottomLeft = new Vector3D(currRow[40 - 4], currRow[41 - 4], currRow[42 - 4]);
                    Vector3D NoseBottomRight = new Vector3D(currRow[79 - 4], currRow[80 - 4], currRow[81 - 4]);

                    double noseDistance = Distance3D(NoseBottomLeft, NoseBottomRight);
                    double outputNose = restNoseWideness - noseDistance;

                    //Console.WriteLine(i+": "+MouthStretchDistance);



                    au13.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au13);
            }

            /*
            * AU14 Dimpler //Ralph
            * MouthLeftCorner = 31 32 33
            * MouthRightCorner = 67 68 69
            */
            {

                _nameAU.Add("Dimpler");
                _numAU.Add("AU14");
                List<double> au14 = new List<double>();

                double restDistance = Distance3D(restMouthLeftCorner, restMouthRightCorner);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D MouthLeftCorner = new Vector3D(currRow[31 - 4], currRow[32 - 4], currRow[33 - 4]);
                    Vector3D MouthRightCorner = new Vector3D(currRow[67 - 4], currRow[68 - 4], currRow[69 - 4]);

                    double distance = Distance3D(MouthLeftCorner, MouthRightCorner);

                    double output = distance - restDistance;

                    au14.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);
                }

                _AUs.Add(au14);
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
             *  AU16 Lower Lip Depressor //Vincent
             *  TES
             */
            {
                _nameAU.Add("Lower Lip Depressor");
                _numAU.Add("AU16");
                List<double> au16 = new List<double>();

                double restDistance = Distance3D(restChinCenter, restMouthLowerlipMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D chinCenter = new Vector3D(currRow[7 - 4], currRow[8 - 4], currRow[9 - 4]);
                    Vector3D MouthLowerMidBottom = new Vector3D(currRow[10 - 4], currRow[11 - 4], currRow[12 - 4]);

                    double auDistance = Distance3D(chinCenter, MouthLowerMidBottom);

                    double output = restDistance - auDistance;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au16.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }


                _AUs.Add(au16);
            }

            /*
             * AU17 Chin Raiser //Vincet
             * TEST
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

                    double output = auDistance - restDistance;

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
                    au23.Add(output);
                    //Console.WriteLine(output);

                }

                _AUs.Add(au23);
            }


            /*
                * AU25 & 27 Mouth Stretch //Ralph
                */
            {
                _nameAU.Add("Lips Part & Mouth Stretch");
                _numAU.Add("AU25 & 27");
                List<double> au27 = new List<double>();

                double restDistance = Distance3D(restMouthUpperlipMidBottom, restMouthLowerlipMidTop);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];


                    Vector3D MouthUpperlipMidbottom = new Vector3D(currRow[94 - 4], currRow[95 - 4], currRow[96 - 4]);
                    Vector3D MouthLowerlipMidtop = new Vector3D(currRow[13 - 4], currRow[14 - 4], currRow[15 - 4]);

                    double MouthStretchDistance = Distance3D(MouthUpperlipMidbottom, MouthLowerlipMidtop);

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = MouthStretchDistance - restDistance;

                    au27.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au27);

            }


            /*
                * AU26 JawDrop //Ralph
                */
            {
                _nameAU.Add("JawDrop");
                _numAU.Add("AU26");
                List<double> au26 = new List<double>();

                double restDistance = Distance3D(restNoseBottom, restChinCenter);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];


                    Vector3D noseBottom = new Vector3D(currRow[16 - 4], currRow[17 - 4], currRow[18 - 4]);
                    Vector3D chinCenter = new Vector3D(currRow[7 - 4], currRow[8 - 4], currRow[9 - 4]);

                    double distance = Distance3D(noseBottom, chinCenter);

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = distance - restDistance;

                    au26.Add(output > 0? output: 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au26);
            }




            /*
            * AU28 Lip Suck //Ralph
            */
            /*
            {
                _nameAU.Add("Lip Suck");
                _numAU.Add("AU28");
                List<double> au28 = new List<double>();
                _AUs.Add(au28);

                double restDistance = Distance3D(restMouthUpperlipMidTop, restMouthLowerlipMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];


                    Vector3D MouthUpperlipMidTop = new Vector3D(currRow[22 - 4], currRow[23 - 4], currRow[24 - 4]);
                    Vector3D MouthLowerlipMidBottom = new Vector3D(currRow[10 - 4], currRow[11 - 4], currRow[12 - 4]);

                    double distance = Distance3D(MouthUpperlipMidTop, MouthLowerlipMidBottom);

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = restDistance - distance;

                    au28.Add(output);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au28);
            }
            */

            /*
            * AU34 Cheek Puffer //Ralph
            */
            {

            }

            /*
            * AU38 Nostril Dilator //Ralph
            * 
            * NoseBottomLeft 40 41 42
            * NoseBottomRight 79 80 81
            */
            /*
            {
                _nameAU.Add("Nostril Dilator");
                _numAU.Add("AU38");
                List<double> au38 = new List<double>();

                double restNoseWidth = Distance3D(restNoseBottomLeft, restNoseBottomRight);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];


                    Vector3D NoseBottomLeft = new Vector3D(currRow[40 - 4], currRow[41 - 4], currRow[42 - 4]);
                    Vector3D NoseBottomRight = new Vector3D(currRow[79 - 4], currRow[80 - 4], currRow[81 - 4]);

                    double NoseWidth = Distance3D(NoseBottomLeft, NoseBottomRight);

                    //Console.WriteLine(i+": "+MouthStretchDistance);

                    double output = NoseWidth - restNoseWidth;

                    au38.Add(output > 0 ? output : 0);

                    //Console.WriteLine(output);

                }

                _AUs.Add(au38);

            }
            */

            /*
             * AU41-44 Eyes Closed Left //Vincent
             */
            {
                _nameAU.Add("Eyes Closed (Left)");
                _numAU.Add("AU41-44L");
                List<double> au44L = new List<double>();


                double restLeftDistance = Distance3D(restLeftEyeMidTop, restLeftEyeMidBottom);
              
                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeMidTop = new Vector3D(currRow[49 - 4], currRow[50 - 4], currRow[51 - 4]);
                    Vector3D LeftEyeMidBottom = new Vector3D(currRow[97 - 4], currRow[98 - 4], currRow[99 - 4]);
             
                    double LeftDistance = Distance3D(LeftEyeMidTop, LeftEyeMidBottom);
             
                    double output = restLeftDistance - LeftDistance;

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au44L.Add(output>0?output:0);
                }

                _AUs.Add(au44L);

            }

            /*
             * AU41-44R Eyes Closed Right //Vincent
             */
            {
                _nameAU.Add("Eyes Closed (Right)");
                _numAU.Add("AU41-44R");
                List<double> au44R = new List<double>();


                double restRightDistance = Distance3D(restRightEyeMidTop, restRightEyeMidBottom);

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D RightEyeMidTop = new Vector3D(currRow[70 - 4], currRow[71 - 4], currRow[72 - 4]);
                    Vector3D RightEyeMidBottom = new Vector3D(currRow[100 - 4], currRow[101 - 4], currRow[102 - 4]);

                    double RightDistance = Distance3D(RightEyeMidTop, RightEyeMidBottom);


                    double output = (restRightDistance - RightDistance);

                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au44R.Add(output > 0 ? output:0);
                }

                _AUs.Add(au44R);

            }






            /*****
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * Rotation AUs
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             * 
             */

            Quaternion restRotQ = new Quaternion(_restRow[1], _restRow[2], _restRow[3], _restRow[0]);

            Vector3D restRotE = ToEulerAngle(restRotQ);



            //Console.WriteLine(restRotQ.X + " " + restRotQ.Y + " " + restRotQ.Z+" "+restRotQ.W);
            // Console.WriteLine(restRotE.X + " " + restRotE.Y + " " + restRotE.Z);

            List<Vector3D> conjugateRotations = new List<Vector3D>();
            


            for (int i = 0; i < _rotationW.Count; i++)
            {
                Quaternion currQ = new Quaternion(_rotationX[i], _rotationY[i], _rotationZ[i],_rotationW[i]);
                Vector3D currE = ToEulerAngle(currQ);
                currE.X = currE.X - restRotE.X;
                currE.Y = currE.Y - restRotE.Y;
                currE.Z = currE.Z - restRotE.Z;

                conjugateRotations.Add(currE);
            }



            /*
             * AU51 Head Turn Left //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
             */
            {
                _nameAU.Add("Head Turn Left");
                _numAU.Add("AU51");
                List<double> au51 = new List<double>();

                for(int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.X;
                    au51.Add(output > 0? output:0);

                }
                _AUs.Add(au51);
            }

            /*
            * AU52 Head Turn Right //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
            */
            {
                _nameAU.Add("Head Turn Right");
                _numAU.Add("AU52");
                List<double> au52 = new List<double>();

                for (int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.X;
                    au52.Add(output < 0 ? -output : 0);

                }
                _AUs.Add(au52);
            }


            /*
             * AU53 Head Up //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
             */
            {
                _nameAU.Add("Head Up");
                _numAU.Add("AU53");
                List<double> au53 = new List<double>();

                for (int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.Y;
                    au53.Add(output > 0 ? output : 0);

                }
                _AUs.Add(au53);
            }

            /*
            * AU54 Head Down //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
            */
            {
                _nameAU.Add("Head Down");
                _numAU.Add("AU54");
                List<double> au54 = new List<double>();

                for (int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.Y;
                    au54.Add(output < 0 ? -output : 0);

                }
                _AUs.Add(au54);
            }

            /*
            * AU55 Head Tilt Left //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
            */
            {
                _nameAU.Add("Head Tilt Left");
                _numAU.Add("AU55");
                List<double> au55 = new List<double>();

                for (int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.Z;
                    au55.Add(output > 0 ? output : 0);

                }
                _AUs.Add(au55);
            }

            /*
            * AU56 Head Turn Right //Vincent  THIS IS NOT THE RIGHT AU LOOK IT UP AND TEST
            */
            {
                _nameAU.Add("Head Tilt Right");
                _numAU.Add("AU56");
                List<double> au56 = new List<double>();

                for (int i = 0; i < conjugateRotations.Count; i++)
                {
                    /*
                     * REMEMBER REST IS 0, 0, 0 degrees
                     */
                    Vector3D currRot = conjugateRotations[i];
                    double output = currRot.Z;
                    au56.Add(output < 0 ? -output : 0);

                }
                _AUs.Add(au56);
            }

            /*
             * 
             * 
             * 
             * 
             * 
             * Move Forward and Back
             * 
             * 
             * 
             * 
             * 
             * 
             */

            /*
            * AU57 Head Forward //Vincent
            */
            {
                _nameAU.Add("Head Forward");
                _numAU.Add("AU57");
                List<double> au57 = new List<double>();


                double restZPosition = restHead.Z;

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D headPoint = new Vector3D(currRow[4 - 4], currRow[5 - 4], currRow[6 - 4]);

                    double ZPosition = headPoint.Z;

                    double output = restZPosition - ZPosition;
                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au57.Add(output > 0?output:0);
                }

                _AUs.Add(au57);

            }

            /*
           * AU58 Head Forward //Vincent
           */
            {
                _nameAU.Add("Head Back");
                _numAU.Add("AU58");
                List<double> au58 = new List<double>();


                double restZPosition = restHead.Z;

                for (int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D headPoint = new Vector3D(currRow[4 - 4], currRow[5 - 4], currRow[6 - 4]);

                    double ZPosition = headPoint.Z;

                    double output = restZPosition - ZPosition;
                    //Console.WriteLine(i+" "+RightDistance + " - " + restRightDistance + " = "+ (RightDistance - restRightDistance));

                    au58.Add(output < 0 ? -output : 0);
                }

                _AUs.Add(au58);

            }

        }

        private double Distance3D(Vector3D A,Vector3D B)
        {
            double deltaX = A.X - B.X;
            double deltaY = A.Y - B.Y;
            double deltaZ = A.Z - B.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }



        public Vector3D ToEulerAngle(Quaternion q)
        {
	        // roll (x-axis rotation)
	        double sinr = +2.0 * (q.W * q.X + q.Y * q.Z);
            double cosr = +1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
            double roll = Math.Atan2(sinr, cosr);

            double pitch;
                // pitch (y-axis rotation)
            double sinp = +2.0 * (q.W * q.Y - q.Z * q.X);
	        if (Math.Abs(sinp) >= 1)
		        pitch = C.math.copysign(Math.PI / 2, sinp); // use 90 degrees if out of range
	        else
		        pitch = Math.Asin(sinp);

                // yaw (z-axis rotation)
                double siny = +2.0 * (q.W * q.Z + q.X * q.Y);
                double cosy = +1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
                double yaw = Math.Atan2(siny, cosy);
            
            Vector3D  d = new Vector3D(roll * (180.0 / Math.PI), pitch * (180.0 / Math.PI), yaw * (180.0 / Math.PI)); //Radians to Degrees
            return d;
        }
        


    }
}