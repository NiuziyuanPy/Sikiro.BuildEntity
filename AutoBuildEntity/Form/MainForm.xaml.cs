﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using 陈珙.AutoBuildEntity.Common.Extension;
using 陈珙.AutoBuildEntity.Common.Helper;
using 陈珙.AutoBuildEntity.Model;
using MessageBox = System.Windows.MessageBox;

namespace 陈珙.AutoBuildEntity.Form
{
    public partial class MainForm : Window
    {
        #region 窗体初始化
        private readonly AutoBuildEntityContent _autoBuildEntityContent;

        private List<string> _hadAddCheckSelectList = new List<string>();

        private List<string> _noAddCheckSelectList = new List<string>();

        private List<string> _noExistCheckSelectList = new List<string>();

        private IEnumerable<ListViewItem> _noAddList;

        private IEnumerable<ListViewItem> _hadAddList;

        private IEnumerable<ListViewItem> _noExistList;

        private readonly string _sqlType;

        public MainForm(AutoBuildEntityContent autoBuildEntityContent, string sqlType)
        {
            InitializeComponent();
            _autoBuildEntityContent = autoBuildEntityContent;
            _sqlType = sqlType;
        }
        #endregion

        #region 加载列表
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //加载列表
            _noAddList =
                _autoBuildEntityContent.TablesName.Where(
                    a => !_autoBuildEntityContent.SelectedProject.CsFilesName.Contains(a.ToCaseCamelName()))
                    .Select(a => new ListViewItem { Name = a });

            _hadAddList =
                _autoBuildEntityContent.TablesName.Where(
                    a => _autoBuildEntityContent.SelectedProject.CsFilesName.Contains(a.ToCaseCamelName()))
                    .Select(a => new ListViewItem { Name = a });

            var classList = _autoBuildEntityContent.TablesName.Select(a => a.ToCaseCamelName()).ToList();
            _noExistList =
                _autoBuildEntityContent.SelectedProject.CsFilesName.Where(
                    a => !classList.Contains(a))
                    .Select(a => new ListViewItem { Name = a });

            NoAddListView.ItemsSource = _noAddList;
            HadAddListView.ItemsSource = _hadAddList;
            NoExistListView.ItemsSource = _noExistList;
        }
        #endregion

        #region 确认提交事件
        /// <summary>
        /// 确认提交事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubmitEvent(object sender, RoutedEventArgs e)
        {
            var theSelectedProject = _autoBuildEntityContent.SelectedProject;
            try
            {
                //获取物理表名
                var addAndUpdateList = _hadAddCheckSelectList.Union(_noAddCheckSelectList).ToList();
                var removeFiles = _noExistCheckSelectList.Select(a => a.ToCaseCamelName() + ".cs").ToList();

                //查询出表结构
                var dbTable = new DbTable(_autoBuildEntityContent.EntityXml.ConnString);
                var dbtables = dbTable.GetTables(addAndUpdateList, _sqlType);

                //根据模版输出
                var templateModel =
                    dbtables.Select(
                        a =>
                            new TemplateModel(a.TableName, a.Columns,
                                theSelectedProject.ProjectName)).ToList();

                var templateDic = templateModel.ToDictionary(a => a.ClassName,
                    item =>
                        NVelocityHelper.ProcessTemplate(_autoBuildEntityContent.EntityXml.EntityTemplate,
                            new Dictionary<string, object> { { "entity", item } }));

                //保存文件
                foreach (var templateData in templateDic)
                {
                    var path = FilesHelper.WriteAndSave(theSelectedProject.ProjectDirectoryName,
                        templateData.Key, templateData.Value);

                    if (_noAddCheckSelectList.Contains(templateData.Key))
                        theSelectedProject.ProjectDte.ProjectItems.AddFromFile(path);
                }

                //添加项目项和排除项目项
                theSelectedProject.ProjectDte.RemoveFilesFromProject(removeFiles);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 全选
        private void HadAddSelectAll_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var checkBoxList = FindVisualChild<CheckBox>(HadAddListView);
            checkBoxList.ForEach(item =>
            {
                item.IsChecked = cb.IsChecked;
            });

            var contentList = checkBoxList.Select(a => a.Tag?.ToString()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            _hadAddCheckSelectList = contentList;
        }

        private void NoAddSelectAll_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var checkBoxList = FindVisualChild<CheckBox>(NoAddListView);
            checkBoxList.ForEach(item =>
            {
                item.IsChecked = cb.IsChecked;
            });

            var contentList = checkBoxList.Select(a => a.Tag?.ToString()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            _noAddCheckSelectList = contentList;
        }

        private void NoExistSelectAll_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var checkBoxList = FindVisualChild<CheckBox>(NoExistListView);
            checkBoxList.ForEach(item =>
            {
                item.IsChecked = cb.IsChecked;
            });

            var contentList = checkBoxList.Select(a => a.Tag?.ToString()).Where(a => !string.IsNullOrEmpty(a)).ToList();
            _noExistCheckSelectList = contentList;
        }

        private List<T> FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            var list = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var item = child as T;
                if (item != null)
                {
                    list.Add(item);
                }
                else
                {
                    var childOfChildren = FindVisualChild<T>(child);
                    if (childOfChildren != null)
                    {
                        list.AddRange(childOfChildren);
                    }
                }
            }
            return list;
        }
        #endregion

        #region CheckBox选中事件
        private void TemplateHadAddCheckBox_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var tbName = cb.Tag.ToString();
            if (cb.IsChecked == true)
            {
                _hadAddCheckSelectList.Add(tbName);
            }
            else
            {
                _hadAddCheckSelectList.Remove(tbName);
            }
        }

        private void TemplateNoAddCheckBox_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var tbName = cb.Tag.ToString();
            if (cb.IsChecked == true)
            {
                _noAddCheckSelectList.Add(tbName);
            }
            else
            {
                _noAddCheckSelectList.Remove(tbName);
            }
        }

        private void TemplateNoExistCheckBox_ClickEvent(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var tbName = cb.Tag.ToString();
            if (cb.IsChecked == true)
            {
                _noExistCheckSelectList.Add(tbName);
            }
            else
            {
                _noExistCheckSelectList.Remove(tbName);
            }
        }
        #endregion

        private void addedSearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            FilterList(addedSearchBox, HadAddListView, _hadAddList);
        }

        private void unAddSearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            FilterList(unAddSearchBox, NoAddListView, _noAddList);
        }

        private void unExistSearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            FilterList(unExistSearchBox, NoExistListView, _noExistList);
        }

        private void FilterList(TextBox tb, ItemsControl clb, IEnumerable<ListViewItem> data)
        {
            var selectInput = tb.Text.ToLower();
            var resultList = data.Where(a => a.Name.ToLower().StartsWith(selectInput));
            clb.ItemsSource = resultList;
        }
    }

    public class ListViewItem
    {
        public string Name { get; set; }
    }
}
