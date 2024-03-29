﻿using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;


namespace ThisIsThePolice_Test
{
    class LoadData
    {

        public String FileLocation;

        public event EventHandler<String> LoadStatus;

        public void OnLoadStatus(object sender, String status)
        {
            LoadStatus?.Invoke(sender, status);
        }

        public async Task<List<GameMission>> GetMissions(int CountMissions = 0)
        {
            List<GameMission> list = new List<GameMission>();

            int x = 0;
            int y = 0;
            int startRow = 1;
            int startCol = 0;

            String[,] sh;

            using (FileStream stream = new FileStream(FileLocation, FileMode.Open, FileAccess.Read))
            {
                var workbook = new HSSFWorkbook(stream);
                var sheet = workbook.GetSheetAt(0);

                x = GetColumn(sheet);
                y = (CountMissions == 0) ? GetRow(sheet) : CountMissions;
                sh = new String[y, x];

                for (int i = startRow; i < y + 1; i++)
                {
                    OnLoadStatus(this, $"Загрузка миссий {i}/{sh.GetLength(0)}");
                    for (int j = startCol; j < x; j++)
                    {
                        string value = GetCellValue(sheet.GetRow(i).GetCell(j));
                        sh[i - startRow, j - startCol] = value;

                        await Task.Delay(1);
                    }
                    await Task.Delay(1);
                }
            }

            for (int c = startCol; c < y; c++)
            {
                GameMission GM = new GameMission
                {
                    Name = sh[c, 0],
                    Description = sh[c, 1].Replace("{NAME}", Environment.UserName),
                    Picture = (sh[c, 2].Contains(".")) ? Image.FromFile(sh[c, 2]) : sh[c, 2].GetImageFromRes(),
                    CopNeed = Convert.ToInt32((sh[c, 3])),
                    Professionalism = short.Parse(sh[c, 4]),
                    Raise = byte.Parse(sh[c, 5]),
                    Slot = byte.Parse(sh[c, 6]),
                    DurationSec = (sh[c, 7] == "") ? -1 : int.Parse(sh[c, 7]),
                    Type = sh[c, 8].GetMissionType(),
                    Place = new GameMission.MissionPlace(sh[c, 9], ushort.Parse(sh[c, 10])),
                    IsTrueCall = (sh[c, 26] == "Yes"),
                    Report = new GameMission.MissionReport(
                        new String[] {
                                sh[c, 19].Replace("{NAME}", Environment.NewLine),
                                sh[c, 20].Replace("{NAME}", Environment.UserName),
                                sh[c, 21].Replace("{NAME}", Environment.UserName),
                        },
                        sh[c, 22].Replace("{NAME}", Environment.UserName),
                        sh[c, 23].Replace("{NAME}", Environment.UserName),
                        sh[c, 24],
                        sh[c, 25],
                        (sh[c, 27] == "") ? null : (sh[c, 27].Contains(".")) ? Image.FromFile(sh[c, 27]) : sh[c, 27].GetImageFromRes()
                    ),
                };

                if (sh[c, 28] != "")
                {
                    String[] Items = sh[c, 28].Split(';');
                    GM.ItemsNeed = new List<GameMission.MissionItems>();
                    foreach (String item in Items)
                        GM.ItemsNeed.Add(new GameMission.MissionItems(item.Split('=')[0], int.Parse(item.Split('=')[1])));
                }

                if (sh[c, 12] != String.Empty)
                {
                    GameMission.MissionComplications complications = new GameMission.MissionComplications
                    {
                        Name = sh[c, 11],
                        Description = sh[c, 12],
                        ResponseOptions = sh[c, 13].Split(';')
                    };

                    if (sh[c, 15] != "")
                    {

                        String[] imgsString = sh[c, 16].Split('-');
                        Image[] imgs = {
                            (imgsString[0].Contains(".")) ? Image.FromFile(imgsString[0]) : imgsString[0].GetImageFromRes(),
                            (imgsString[1].Contains(".")) ? Image.FromFile(imgsString[1]) : imgsString[1].GetImageFromRes(),
                            (imgsString[2].Contains(".")) ? Image.FromFile(imgsString[2]) : imgsString[2].GetImageFromRes()
                        };

                        GameMission.MissionComplications.AdditionComplication Addition = new GameMission.MissionComplications.AdditionComplication
                        {
                            Pictures = imgs,
                            Descriptions = sh[c, 14].Split(';'),
                            ResponseOptions = new String[] {
                                      sh[c, 15].Split('-')[0],
                                      sh[c, 15].Split('-')[1],
                                      sh[c, 15].Split('-')[2]
                            }
                        };
                        complications.Addition = Addition;
                    }

                    String[] tempString = sh[c, 17].Split('-');
                    int[,] tempByte = new int[tempString.Length, 3];
                    for (int i = 0; i < tempByte.GetLength(0); i++)
                    {
                        tempByte[i, 0] = (tempString.Length - 1 >= i && tempString[i] != "" && tempString[i].Split(';').Length >= 1) ? int.Parse(tempString[i].Split(';')[0]) : -1;
                        tempByte[i, 1] = (tempString.Length - 1 >= i && tempString[i] != "" && tempString[i].Split(';').Length >= 2) ? int.Parse(tempString[i].Split(';')[1]) : -1;
                        tempByte[i, 2] = (tempString.Length - 1 >= i && tempString[i] != "" && tempString[i].Split(';').Length >= 3) ? int.Parse(tempString[i].Split(';')[2]) : -1;
                    }
                    complications.CorrectAnswerOptions = tempByte;
                    complications.Picture = (sh[c, 18].Contains(".")) ? Image.FromFile(sh[c, 18]) : sh[c, 18].GetImageFromRes();

                    GM.Complications = complications;
                }

                list.Add(GM);
            }

            await Task.Delay(1);

            return list;
        }

        private string GetCellValue(ICell cell)
        {
            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.String:
                    return cell.StringCellValue;
                default:
                    return "";
            }
        }

        public async Task<List<Cop>> GetCops(int CountCop = 0)
        {
            List<Cop> list = new List<Cop>();

            int x = 0;
            int y = 0;
            int startRow = 1;
            int startCol = 0;

            String[,] sh;

            try
            {
                using (FileStream stream = new FileStream(FileLocation, FileMode.Open))
                {
                    var workbook = new HSSFWorkbook(stream);
                    var sheet = workbook.GetSheetAt(0);

                    x = GetColumn(sheet);
                    y = (CountCop == 0) ? GetRow(sheet) : CountCop;
                    sh = new String[y, x];

                    for (int i = startRow; i < y + 1; i++)
                    {
                        OnLoadStatus(this, $"Загрузка копов {i}/{sh.GetLength(0)}");

                        for (int j = startCol; j < x; j++)
                        {
                            string value = GetCellValue(sheet.GetRow(i).GetCell(j));
                            sh[i - startRow, j - startCol] = value;

                            await Task.Delay(1);
                        }
                    }

                    await Task.Delay(1);
                }

                for (int c = startCol; c < y; c++)
                {
                    Cop cop = new Cop
                    {
                        FirstName = sh[c, 0],
                        LastName = sh[c, 1],
                        Photo = (sh[c, 3].Contains(".")) ? Image.FromFile(sh[c, 3]) : sh[c, 3].GetImageFromRes(),
                        Gender = sh[c, 4].GetGender(),
                        Race = sh[c, 5].GetRace(),
                        Views = sh[c, 6].GetPoliticalViews(),
                        Professionalism = int.Parse(sh[c, 7]),
                        RaiseCount = byte.Parse(sh[c, 8]),
                        Energy = Convert.ToInt32(sh[c, 9]),
                        IsAlcoholic = (sh[c, 10] == "Yes"),
                        IsOld = (sh[c, 11] == "Yes"),
                        IsWeed = (sh[c, 12] == "Yes")
                    };
                    cop.ID = sh[c, 2] == "" ? cop.GenerationCopID() : sh[c, 2];
                    cop.ID = (cop.ID.Length == 1) ? $"000{cop.ID}" :
                             (cop.ID.Length == 2) ? $"00{cop.ID}" :
                             (cop.ID.Length == 3) ? $"0{cop.ID}" :
                              cop.ID;

                    list.Add(cop);
                }
            }
            catch { }

            await Task.Delay(1);

            return list;
        }

        public async Task GetStorage(int CountItem = 0)
        {
            int x = 0;
            int y = 0;
            int startRow = 1;
            int startCol = 0;

            String[,] sh;

            try
            {
                using (FileStream stream = new FileStream(FileLocation, FileMode.Open))
                {
                    var workbook = new HSSFWorkbook(stream);
                    var sheet = workbook.GetSheetAt(0);

                    x = GetColumn(sheet);
                    y = (CountItem == 0) ? GetRow(sheet) : CountItem;
                    sh = new String[y, x];

                    for (int i = startRow; i < y + 1; i++)
                    {
                        OnLoadStatus(this, $"Загрузка склада {i}/{sh.GetLength(0)}");

                        for (int j = startCol; j < x; j++)
                        {
                            string value = GetCellValue(sheet.GetRow(i).GetCell(j));
                            sh[i - startRow, j - startCol] = value;

                            await Task.Delay(1);
                        }
                    }

                    await Task.Delay(1);
                }

                for (int c = startCol; c < y; c++)
                {

                    // Colors  --->
                    Color[] colors = null;
                    if (sh[c, 6] != "")
                    {
                        String[] ColorsSplit = sh[c, 6].Split(';');
                        String[,] ColorsString = new string[ColorsSplit.Length, 3];
                        for (int i = 0; i < ColorsSplit.Length; i++)
                        {
                            String[] split = ColorsSplit[i].Split(',');
                            ColorsString[i, 0] = split[0];
                            ColorsString[i, 1] = split[1];
                            ColorsString[i, 2] = split[2];
                        }
                        int[,] colorsInt = new int[ColorsSplit.Length, 3];
                        for (int i = 0; i < ColorsSplit.Length; i++)
                        {
                            colorsInt[i, 0] = int.Parse(ColorsString[i, 0]);
                            colorsInt[i, 1] = int.Parse(ColorsString[i, 1]);
                            colorsInt[i, 2] = int.Parse(ColorsString[i, 2]);
                        }
                        colors = new Color[ColorsSplit.Length];
                        for (int i = 0; i < ColorsSplit.Length; i++)
                            colors[i] = Color.FromArgb(colorsInt[i, 0], colorsInt[i, 1], colorsInt[i, 2]);
                    }
                    // <--- Colors

                    // Images --->
                    String[] ImageString = sh[c, 4].Split(';');
                    Image[] images = new Image[ImageString.Length];
                    for (int i = 0; i < images.Length; i++)
                        images[i] = (ImageString[i].Contains(".")) ? Image.FromFile(ImageString[i]) : ImageString[i].GetImageFromRes();
                    // <--- Images

                    Dictionary<Color, Image> dic = null;
                    if (images.Length > 1)
                    {
                        dic = new Dictionary<Color, Image>();
                        for (int i = 0; i < images.Length; i++)
                            dic.Add(colors[i], images[i]);
                    }

                    StorageItem Item = new StorageItem(
                        (colors != null) ? sh[c, 1].ToLower() : sh[c, 1],
                             (colors != null) ? sh[c, 1].ToLower() : sh[c, 1],
                        images,
                        int.Parse(sh[c, 2]),
                        int.Parse(sh[c, 3]),
                        (sh[c, 5] == "Legal") ? StorageItem.ItemType.Legal : (sh[c, 5] == "Trash") ? StorageItem.ItemType.Trash : StorageItem.ItemType.Illegal,
                        colors,
                        dic,
                        (sh[c, 7] == "") ? null : sh[c, 7].Split(';'),
                        Color.Transparent
                    );
                    if (colors != null)
                    {
                        Item.Name = $"{Item.ColorsName[0]} {Item.NameStatic}";
                        Item.ChosenColor = colors[0];
                    }

                    DataTransfer.Items.Add(new StorageItem(Item));

                    StorageItem NewItem = new StorageItem(Item, Item.Count);
                    if (sh[c, 0] == "0")
                        DataTransfer.StorageItems.Add(NewItem);
                    else if (NewItem.Type == StorageItem.ItemType.Trash)
                        DataTransfer.Seller1Items.Add(NewItem);
                    else if (NewItem.Type == StorageItem.ItemType.Legal)
                        DataTransfer.Seller2Items.Add(NewItem);
                    else if (NewItem.Type == StorageItem.ItemType.Illegal)
                        DataTransfer.Seller3Items.Add(NewItem);
                }
            }
            catch { }

            await Task.Delay(1);
        }

        private int GetColumn(ISheet sheet)
        {
            return sheet.GetRow(0).LastCellNum;
        }

        private int GetRow(ISheet sheet)
        {
            for (int i = 0; i < sheet.LastRowNum; i++)
            {
                var value = GetCellValue(sheet.GetRow(i).GetCell(0));
                if (string.IsNullOrEmpty(value))
                    return i;
            }

            return sheet.LastRowNum;
        }
    }
}
