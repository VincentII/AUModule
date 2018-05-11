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
                    auRow.Add(_AUs[i][rowIndex]);
                
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




            /*
             * Initialize rest points
             */
            Vector3D restLeftEyeInner =         new Vector3D(_restRow[43], _restRow[44], _restRow[45]);
            Vector3D restLeftEyeBrowInner =     new Vector3D(_restRow[52], _restRow[53], _restRow[54]);
            Vector3D restRightEyeBrowInner =    new Vector3D(_restRow[82], _restRow[83], _restRow[84]);
            Vector3D restRightEyeInner =           new Vector3D(_restRow[85], _restRow[86], _restRow[87]);




            /*
             *  AU1 Inner brow raiser
             */
            {
                _nameAU.Add("InnerBrowRiser");
                _numAU.Add("AU1");
                List <double> au1 = new List<double>();

                double restLeftDistance = Distance3D(restLeftEyeInner, restLeftEyeBrowInner);
                double restRightDistance = Distance3D(restRightEyeInner, restRightEyeBrowInner);

                for(int i = 0; i < _rowsCSV.Count; i++)
                {
                    List<double> currRow = _rowsCSV[i];

                    Vector3D LeftEyeInner = new Vector3D(currRow[43 - 4], currRow[44 - 4], currRow[45 - 4]);
                    Vector3D LeftEyeBrowInner = new Vector3D(currRow[52 - 4], currRow[53 - 4], currRow[54 - 4]);
                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83-4], currRow[84 - 4]);
                    Vector3D RightEyeInner = new Vector3D(currRow[85 - 4], currRow[86 - 4], currRow[87 - 4]);

                    double LeftDistance = Distance3D(LeftEyeInner, LeftEyeBrowInner);
                    double RightDistance = Distance3D(RightEyeInner, RightEyeBrowInner);

                    double output = ((LeftDistance - restLeftDistance)+(RightDistance - restRightDistance))/2;
                    
                    

                    au1.Add(output > 0 ? output: 0);



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
                    Vector3D RightEyeBrowInner = new Vector3D(currRow[82 - 4], currRow[83-4], currRow[84 - 4]);
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

            /*
             * AU27 Mouth Stretch //Ralph
             */
            {
                _nameAU.Add("MouthStretch");
                _numAU.Add("AU27");
                List<double> au27 = new List<double>();
                _AUs.Add(au27);

            }


            for (int i = 0; i < _AUs[0].Count; i++)
            {
                Console.WriteLine(_AUs[0][i] + " " + _AUs[1][i]);
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