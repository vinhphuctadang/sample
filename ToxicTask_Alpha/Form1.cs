using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace ToxicTask_Alpha
{
    public partial class Form1 : Form
    {
        
        //Dictionary<string, string> chosenSubjectClassIDs;
        
        Stack< KeyValuePair<int, int>> dfsState;            
        Dictionary<String, List<SubjectClass>> database;
        List<SubjectClass> chosenSubjectClass;
        List<string> studyPlanBySubjectCode;
        List<List<SubjectClass>> generatedList;
        private int focusGeneratedListIndex = 0;
        List<string> subjectCodeList; // for testing only

        public Form1()
        {
            InitializeComponent();
            dfsState = new Stack<KeyValuePair<int, int>>();
            chosenSubjectClass = new List<SubjectClass>();
            timeTable.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            timeTable.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            timeTable.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //OnLoadDatabase("E:\\DanhSachHocPhan.XLS");
            //OnLoadAllSubjects();
            //string[] res = getFilterData();
            //timeTable.Rows.Add();
            //for (int i = 0; i<7; ++i)
            //{
            //    timeTable[i, 0].Value = res[i];
            //}
        }


        private void OnLoadAllSubjects ()
        {
            Microsoft.Office.Interop.Excel.Application excelApplication = new Microsoft.Office.Interop.Excel.Application();
            Console.WriteLine("Loading subject codes of Aquatic Institue");

            var databaseBook = excelApplication.Workbooks.Add("E:\\Subjects.xlsx");
            var sheet = databaseBook.Sheets[1];
            object[,] subjectCodes = sheet.Range("B1:B78").Value2;
            subjectCodeList = new List<string>();

            for (int i = subjectCodes.GetLowerBound(0); i <= subjectCodes.GetUpperBound(0); ++i)
            {
                subjectCodeList.Add(subjectCodes[i, subjectCodes.GetLowerBound(1)].ToString());
            }

            databaseBook.Close(false);
            excelApplication.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApplication);
        }
        private void OnLoadDatabase(String excelFileDirectory)
        {
            statusBar.Text = "Opening " + excelFileDirectory;                            
            var excelApplication = new Microsoft.Office.Interop.Excel.Application();
            var databaseBook = excelApplication.Workbooks.Add(excelFileDirectory);
            
            excelApplication.Visible = false;
            
            // read from row id 3
            // read from column id 2
            // Subject code: AQ203 - Column 2
            // Subject sign: 01, 02 ... Column 3
            // Subject present day: Column 4
            // Subject class start period: Column 5
            // Subject class number of period: Column 6
            // Subject class present room: Column 7
            // Subject name:  Column 11

            var activeWorkSheet = databaseBook.Sheets[1];

            object[,] openSubjectClassData = activeWorkSheet.Range("A3", "L" + activeWorkSheet.UsedRange.Rows.Count.ToString()).Value2;
            databaseBook.Close (false);
            excelApplication.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApplication);

            database = new Dictionary<string, List<SubjectClass>>();

            statusBar.Text = "Converting format of " + excelFileDirectory;
            int rowIndex = openSubjectClassData.GetLowerBound (0);
            int targetIndex = openSubjectClassData.GetUpperBound(0);
            string prevClassID = "";
            while (rowIndex <= targetIndex)
            {
                List <SubjectClass> subjectCode = null;
                object subjectCell = openSubjectClassData[rowIndex, 7];
                if (subjectCell == null)
                {
                    ++rowIndex;
                    continue;
                }

                database.TryGetValue(openSubjectClassData[rowIndex, 2].ToString (), out subjectCode);
                
                if (subjectCode == null)
                {
                    subjectCode = new List <SubjectClass>();
                    database.Add(openSubjectClassData[rowIndex, 2].ToString(), subjectCode);
                    prevClassID = "";
                }
                 
                SubjectClass focusSubjectClass = null;
                string classCode = openSubjectClassData[rowIndex, 3].ToString();
                if (!prevClassID.Equals(classCode))
                {
                    prevClassID = classCode;
                    focusSubjectClass = new SubjectClass(openSubjectClassData[rowIndex, 11].ToString(),
                        openSubjectClassData[rowIndex, 3].ToString());
                    subjectCode.Add(focusSubjectClass);
                }
                else
                {
                    focusSubjectClass = subjectCode[subjectCode.Count - 1];
                }

                string debug = rowIndex.ToString() + " " 
                    + openSubjectClassData[rowIndex, 4].ToString() + " "
                    + openSubjectClassData[rowIndex, 5].ToString() + " "
                    + openSubjectClassData[rowIndex, 6].ToString() + " "
                    + openSubjectClassData[rowIndex, 7].ToString();
                    
                Console.Write(debug);
                try
                {
                    Range tmp; 
                    focusSubjectClass.AddRange(tmp = new Range(
                    int.Parse (openSubjectClassData[rowIndex, 4].ToString ()),
                    int.Parse (openSubjectClassData[rowIndex, 5].ToString ()),
                    int.Parse (openSubjectClassData[rowIndex, 6].ToString ()),
                    openSubjectClassData[rowIndex, 7].ToString()
                    ));
                    //Console.WriteLine(" " + tmp.startPivot.ToString () + " " + tmp.endPivot.ToString ());                    

                } catch (Exception exception)
                {

                }
                
                ++rowIndex;
            }
            
            statusBar.Text = "Imported " + excelFileDirectory;
        }
        
        bool CanAddASubject (SubjectClass subjectClass)
        {
            foreach (var tmpSubjectClass in chosenSubjectClass)
            {

                if (tmpSubjectClass.IsIntersect(subjectClass))
                    return false;
            }
            return true;
        }
        
        int CollectStudentStudyPlan (out string error) // from plan subjects view
        {
            //subjects[]
            error = "";
            studyPlanBySubjectCode = new List<string>();
            for (int i = 0; i<subjects.RowCount;++i)
            {
                List<SubjectClass> resultValue;
                if (subjects[0, i].Value != null)
                    if (!database.TryGetValue(subjects[0, i].Value.ToString().ToUpper(), out resultValue))
                    {
                        if (error.Equals(""))
                            error = "Không mở học phần ";
                        error += subjects[0, i].Value.ToString().ToUpper()+" ";
                    } else 
                        studyPlanBySubjectCode.Add(subjects[0, i].Value.ToString ().ToUpper ());
            }

            if (studyPlanBySubjectCode.Count == 0)
            {
                error += "Không có kế hoạch học tập";
                return -1; // CollectErr_NotFoundAnyCorrect
            }
                
            return 0;
        }

        void BeginTimeTableGeneration ()
        {
            dfsState.Clear ();
            dfsState.Push (new KeyValuePair<int, int>(0, -1));
            chosenSubjectClass.Clear();
        }

        protected string[,] PrepareTimeTable (List<SubjectClass> classes) // 
        {
            string[,] resultTimeTable = new string[9, 7];
            foreach (var aClass in classes)
            {
                foreach (var aPeriod in aClass.GetClassTimes())
                {
                    int col, fromRow, toRow;
                    aPeriod.ToDayStartEnd(out col, out fromRow, out toRow);
                    for (int i = fromRow; i <= toRow; ++i)
                    {
                        //Console.WriteLine();
                        resultTimeTable[i, col] = aClass.ToString() + " " + aPeriod.ToString();
                    }
                }
            }
            return resultTimeTable;
        }
        protected void ShowGeneratedTimeTable (string[,] table)
        {
            timeTable.Rows.Clear();

            for (int i = timeTable.Rows.Count; i <= 9; ++i)
                timeTable.Rows.Add();

            int col = 0, row = 0;
            for (int i = table.GetLowerBound (0); i<=table.GetUpperBound (0); ++i)
            {

                for (int j = table.GetLowerBound (1); j <= table.GetUpperBound (1); ++j)
                {
                    timeTable[col++, row].Value = table[i, j];
                }
                col = 0;
                ++row;
            }
            // TODO: Command DatagridView to show
        }
        
        bool GenerateNextTimeTable () // respect the dfsState
        {
            while (dfsState.Count > 0)
            {
                KeyValuePair<int, int> cache = dfsState.Pop();
                int focusSubjectClassIndex = cache.Value; // index in 
                int focusSubjectCodeIndex = cache.Key; // index in dfs depth
                // Console.WriteLine("DFS depth " + focusSubjectCodeIndex.ToString());

                if (focusSubjectCodeIndex >= studyPlanBySubjectCode.Count)
                {
                    return true;
                    // TO DO: reports found one suitable time table
                }

                List<SubjectClass> subjectClasses =
                database[studyPlanBySubjectCode[focusSubjectCodeIndex]];

                if (focusSubjectClassIndex >= 0)
                {
                    chosenSubjectClass.Remove(subjectClasses[focusSubjectClassIndex]);
                    //Console.WriteLine("REMOVED " + subjectClasses[focusSubjectClassIndex].ToString());
                }

                while (focusSubjectClassIndex + 1 < subjectClasses.Count)
                {
                    ++focusSubjectClassIndex;
                    
                    if (CanAddASubject(subjectClasses[focusSubjectClassIndex]))
                    {
                        
                        chosenSubjectClass.Add(subjectClasses[focusSubjectClassIndex]);
                        //Console.WriteLine("ADDED " + subjectClasses[focusSubjectClassIndex].ToString());
                        dfsState.Push(new KeyValuePair<int, int>(focusSubjectCodeIndex, focusSubjectClassIndex));
                        dfsState.Push(new KeyValuePair<int, int>(focusSubjectCodeIndex + 1, -1));
                        break;
                    }
                }
            }
            return false;
        }

        string[] getFilterData ()
        {
            var result = new string[7];
            
            foreach (string code in subjectCodeList)
            {
                List<SubjectClass> theClasses = null;
                database.TryGetValue(code, out theClasses);
                                
                if (theClasses != null)
                foreach (var aClass in theClasses)
                {
                    foreach (var aPeriod in aClass.GetClassTimes())
                    {
                        int day = 0, fromPeriod = 0, toPeriod = 0;
                        aPeriod.ToDayStartEnd(out day, out fromPeriod, out toPeriod);
                        fromPeriod++;
                        toPeriod++;
                        string ans = code + " " + aClass.ToString () + " " + 
                                aPeriod.ToString() + " " + fromPeriod.ToString () + " " 
                                + toPeriod.ToString ();
                        result[day] += ans + Environment.NewLine;
                    }
                }
            }
            return result;
        }
        private void loadDatabase_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "(*.xls)|*.xls|(*.xlsx)|*.xlsx";
            var openFileReuslt = openFileDialog.ShowDialog(this);
            if (openFileReuslt == DialogResult.OK) {
                fileDirectory.Text = openFileDialog.FileName;
                statusBar.Text = openFileDialog.FileName;
                OnLoadDatabase(openFileDialog.FileName);
            }
        }

        private void generate_Click(object sender, EventArgs e)
        {
            string error = "";
            if (CollectStudentStudyPlan (out error) == -1)
            {
                MessageBox.Show(error);
                return;
            };

            if (!error.Equals(""))
            {
                MessageBox.Show(error);
            }

            generatedList = new List<List<SubjectClass>>();

            BeginTimeTableGeneration ();
            OnGenerationCalled();
        }

        protected bool OnGenerationCalled ()
        {
            
            if (GenerateNextTimeTable())
            {
                var table = PrepareTimeTable(chosenSubjectClass);
                List<SubjectClass> tempoary = new List<SubjectClass>();
                foreach (var child in chosenSubjectClass)
                {
                    tempoary.Add(child);
                }
                ShowGeneratedTimeTable(table);
                generatedList.Add(tempoary);
                return true;
            }

            MessageBox.Show("Không tìm thấy thời khóa biểu thích hợp nữa");
            return false;
        }
            

        private void button1_Click(object sender, EventArgs e)
        {
            if (focusGeneratedListIndex + 1 >= generatedList.Count)
            {
                if (OnGenerationCalled())
                    focusGeneratedListIndex++;
            } else
            {
                ShowGeneratedTimeTable(PrepareTimeTable(generatedList[++focusGeneratedListIndex]));

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (focusGeneratedListIndex > 0)
            {
                ShowGeneratedTimeTable(PrepareTimeTable(generatedList[--focusGeneratedListIndex]));
            } else
            {
                MessageBox.Show("Không có thời khóa biểu trước");
            }
        }
    }

    public class Range
    {
        public
            int startPivot, endPivot;
        private
            string room = "";
        
        public Range (int day, int startPeriod, int numberOfPeriod, string room) // 9 period per day
        {
            startPivot = (day - 2) * 9 + startPeriod - 1;
            endPivot = startPivot + numberOfPeriod - 1;
            this.room = room;
        }

        public override string ToString()
        {
            return room.ToString ();
        }

        public void ToDayStartEnd (out int day, out int start, out int end)
        {
            day = startPivot / 9;
            start = startPivot % 9;
            end = endPivot % 9;
        }

        public bool IsValid ()
        {
            return startPivot <= endPivot;
        }

        public bool IsIntersect (Range other)
        {
            return (!(other.endPivot < startPivot || endPivot < other.startPivot));
        }
    }

    public class SubjectClass
    {

        private List<Range> times;
        private String name;
        private String id;

        public SubjectClass (String name, String id)
        {

            this.name = name;
            this.id = id;
            times = new List<Range>();
        }

        public SubjectClass (List<Range> timeList, String name, String id)
        {
            this.id = id;
            this.name = name;
            times = new List<Range>(timeList);
        }

        public void AddRange (Range range)
        {
            times.Add(range);
        }
        
        public List<Range> GetClassTimes ()
        {
            return times;
        }
        public override string ToString()
        {
            return name + " " + id;
        }
        public bool IsIntersect (SubjectClass other)
        {
            var otherClassTimes = other.GetClassTimes();
            foreach (Range thisClassPeriod in times)
            {
                foreach (Range otherClassPeriod in otherClassTimes)
                {
                    if (thisClassPeriod.IsIntersect(otherClassPeriod))
                        return true;
                }
            }
            return false;
        }
    }


}
