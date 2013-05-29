using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

using BindableDataGrid.Data;
using GlobalAdvanceFind_SL.CrmSDK;

namespace GlobalAdvanceFind_SL
{
    public partial class MainPage : UserControl
    {

        XDocument xdoc;
        XElement xfetchXml;
        private List<QuickSearchFetchXml> qsfetchXmlList = new List<QuickSearchFetchXml>();
        private string ServerBaseUrl = string.Empty;

        public MainPage()
        {
            InitializeComponent();
            textBoxSearch.IsEnabled = false;
            ServerBaseUrl = SilverlightUtility.GetServerBaseUrl().ToString();
            RetrieveQuickSerchFetchXml();
        }

        private void RetrieveQuickSerchFetchXml()
        {
            try
            {
                QueryExpression query = new QueryExpression()
                {
                    EntityName = "gqs_globalquicksearchconfig",
                    ColumnSet = new ColumnSet()
                    {
                        Columns = new System.Collections.ObjectModel.ObservableCollection<string>(new string[] { "gqs_name", "gqs_quicksearchfetchxml", "gqs_quicksearchformattedfetchxml" })
                    },
                    Criteria = new FilterExpression()
                };

                query.Criteria.Conditions.Add(new ConditionExpression()
                {
                    AttributeName = "gqs_quicksearchformattedfetchxml",
                    Operator = ConditionOperator.NotNull
                    //Values = new System.Collections.ObjectModel.ObservableCollection<object>(new object[] { 1,2,3,4,8,112 })
                });
                query.Criteria.Conditions.Add(new ConditionExpression()
                {
                    AttributeName = "statecode",
                    Operator = ConditionOperator.Equal,
                    Values = new System.Collections.ObjectModel.ObservableCollection<object>(new object[] {0})
                });

                OrganizationRequest request = new OrganizationRequest() { RequestName = "RetrieveMultiple" };
                request["Query"] = query;

                //MessageBox.Show(SilverlightUtility.GetServerBaseUrl().ToString());

                IOrganizationService service = SilverlightUtility.GetSoapService();

                service.BeginExecute(request, new AsyncCallback(QuickSerchFetchXml_Callback), service);
            }
            catch (Exception ex)
            {
                this.ReportError(ex);
            }
        }

        private void QuickSerchFetchXml_Callback(IAsyncResult result)
        {
            try
            {
                OrganizationResponse response = ((IOrganizationService)result.AsyncState).EndExecute(result);
                EntityCollection results = (EntityCollection)response["EntityCollection"];
                foreach (Entity entity in results.Entities)
                {
                    QuickSearchFetchXml qsfx = new QuickSearchFetchXml();

                    qsfx.EntityLogicalName = entity.GetAttributeValue<string>("gqs_name");
                    qsfx.TransformedFetchXml = entity.GetAttributeValue<string>("gqs_quicksearchformattedfetchxml");
                    qsfx.FetchXml = entity.GetAttributeValue<string>("gqs_quicksearchfetchxml");

                    //Check for duplicates
                    if (qsfetchXmlList.Where<QuickSearchFetchXml>(a => a.EntityLogicalName.ToUpper() == qsfx.EntityLogicalName.ToUpper()).Count() == 0)
                    {
                        this.qsfetchXmlList.Add(qsfx);
                    }
                }

                this.Dispatcher.BeginInvoke(() => textBoxSearch.IsEnabled = true);
            }
            catch (Exception ex)
            {
                this.ReportError(ex);
            }
        }

        private void ReportError(Exception ex)
        {
            this.ReportMessage("Exception: " + SilverlightUtility.ConvertToString(ex));
        }

        private void ReportMessage(string message)
        {
            this.Dispatcher.BeginInvoke(() => ResultsLabel.Content = message);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            GlobalSearch();
        }

        private void GlobalSearch()
        {
            SearchResultGridPanel.Children.Clear();
            try
            {
                if (this.qsfetchXmlList.Count() == 0)
                {
                    ReportMessage("No Entities configured for Quick Search. Go to 'Global Quick Search Config' entity and add entities that needs Quick Search.");
                    return;
                }
                if (textBoxSearch.Text.Trim().Length == 0)
                {
                    ReportMessage("Enter search text");
                    return;
                }
                string searchText = string.Format("%{0}%", textBoxSearch.Text.Trim());

                foreach (QuickSearchFetchXml qsfx in this.qsfetchXmlList)
                {
                    qsfx.FormattedFetchXml = string.Format(qsfx.TransformedFetchXml, searchText);
                    GetEntityRecords(qsfx);
                }
            }
            catch (Exception Ex)
            {
                this.ReportError(Ex);
            }
        }

        private void GetEntityRecords(QuickSearchFetchXml qsfx)
        {
            string xml = CreateXml(qsfx.FormattedFetchXml, qsfx.PagingCookie, qsfx.PageNumber == 0 ? 1 : qsfx.PageNumber, 5);

            FetchExpression query = new FetchExpression();
            query.Query = xml;

            OrganizationRequest request = new OrganizationRequest() { RequestName = "RetrieveMultiple" };
            request["Query"] = query;
            IOrganizationService service = SilverlightUtility.GetSoapService();

            AsynchMethodParameters param = new AsynchMethodParameters
            {
                Service = service,
                QSFetchXml = qsfx
            };

            service.BeginExecute(request, new AsyncCallback(GetEntities_Callback), param);
        }

        private void GetEntities_Callback(IAsyncResult result)
        {
            try
            {
                AsynchMethodParameters param = (AsynchMethodParameters)result.AsyncState;

                OrganizationResponse response = param.Service.EndExecute(result);
                EntityCollection results = (EntityCollection)response["EntityCollection"];
                param.QSFetchXml.PagingCookie = results.PagingCookie;
                param.QSFetchXml.HasMoreRecords = results.MoreRecords;

                //this.Dispatcher.BeginInvoke(() => SetNavButtons(results.MoreRecords));

                xdoc = XDocument.Parse(param.QSFetchXml.FormattedFetchXml);

                var vv = xdoc.Root.Elements("entity").Descendants("attribute").ToList<XElement>();
                //var vv = xfetchXml.Elements("entity").Descendants("attribute");

                DataTable dt = new DataTable("EntityDataTable");

                string pkColumn = vv.Where(a => a.Attribute("isPk") != null && Convert.ToBoolean(a.Attribute("isPk").Value) == true).First<XElement>().Attribute("name").Value;

                int _columnIndex = 0;

                foreach (var v in vv.OrderBy(a=> Int32.Parse(a.Attribute("displayOrder") != null ? a.Attribute("displayOrder").Value : "1000")))
                {
                    // Create a column
                    DataColumn dc1 = new DataColumn(v.Attribute("name").Value);
                    dc1.Caption = v.Attribute("displayName") != null ? v.Attribute("displayName").Value : v.Attribute("name").Value;
                    dc1.ReadOnly = true;
                    if (v.Attribute("isPk") != null && Convert.ToBoolean(v.Attribute("isPk").Value))
                    {
                        dc1.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        if (_columnIndex == 0)
                        {
                            dc1.DataType = typeof(HyperlinkButton);
                            dc1.NavigateURLBindingColumn = pkColumn;
                        }
                        else
                        {
                            dc1.DataType = typeof(string);
                        }
                        _columnIndex++;
                    }
                    dc1.AllowResize = true;
                    dc1.AllowSort = true;
                    dc1.AllowReorder = true;

                    if (v.Attribute("columnWidth") != null)
                    {
                        dc1.ColumnWidth = Double.Parse(v.Attribute("columnWidth").Value);
                    }

                    dt.Columns.Add(dc1);
                }

                #region For--Each - Populating DataRows 
                foreach (Entity e in results.Entities)
                {
                    DataRow dr = new DataRow();

                    for (int i = 0; i < e.Attributes.Count; i++)
                    {
                        if (e.Attributes[i].Value.GetType() == typeof(AliasedValue))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = ((AliasedValue)e.Attributes[i].Value).Value.ToString();
                        }
                        else if (e.Attributes[i].Value.GetType() == typeof(EntityReference))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = ((EntityReference)e.Attributes[i].Value).Name;
                        }
                        else if (e.Attributes[i].Value.GetType() == typeof(OptionSetValue))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = e.FormattedValues.GetItem(e.Attributes[i].Key);
                        }
                        else if (e.Attributes[i].Value.GetType() == typeof(Money))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = e.FormattedValues.GetItem(e.Attributes[i].Key);
                        }
                        else if (e.Attributes[i].Value.GetType() == typeof(Boolean))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = e.FormattedValues.GetItem(e.Attributes[i].Key);
                        }
                         //datetime
                        else if (e.Attributes[i].Value.GetType() == typeof(DateTime))
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = e.FormattedValues.GetItem(e.Attributes[i].Key);
                        }
                        //"partyid"
                        else if (e.Attributes[i].Value.GetType() == typeof(EntityCollection))
                        {
                            string _str = string.Empty;

                            EntityCollection ec = (EntityCollection)e.Attributes[i].Value;
                            foreach( Entity activityPointer in ec.Entities)
                            {
                                if (activityPointer.GetAttributeValue<EntityReference>("partyid") != null)
                                {
                                    _str += string.Format("{0};", activityPointer.GetAttributeValue<EntityReference>("partyid").Name);
                                }
                            }
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = _str.TrimEnd(new char[]{';'});
                        }
                        else
                        {
                            dr[e.Attributes[i].Key.Replace(".", string.Empty)] = e.Attributes[i].Value.ToString();
                        }
                    }

                    dt.Rows.Add(dr);
                }

                this.Dispatcher.BeginInvoke(() => DataBind(dt, param.QSFetchXml));

                #endregion
            }
            catch (Exception Ex)
            {
                ReportError(Ex);
            }
        }

        private void DataBind(DataTable dt, QuickSearchFetchXml qsfx)
        {
            string stackPanelName = string.Format("StackPanel_{0}", qsfx.EntityLogicalName);
            // Create a DataSet and add the table to it
            DataSet ds = new DataSet("EntityDataSet");
            ds.Tables.Add(dt);

            BindableDataGrid.BindableDataGrid myBindableDG = new BindableDataGrid.BindableDataGrid();

            var stackPanel = SearchResultGridPanel.Children.Where(a => a.GetType() == typeof(StackPanel) && ((StackPanel)a).Name == stackPanelName).FirstOrDefault<UIElement>();

            if (stackPanel != null)
            {
                var dataGrid = ((StackPanel)stackPanel).Children.Where(a => a.GetType() == typeof(BindableDataGrid.BindableDataGrid) && ((BindableDataGrid.BindableDataGrid)a).Name == qsfx.DataGridName);
                if (dataGrid != null && dataGrid.Count() == 1)
                {
                    myBindableDG = (BindableDataGrid.BindableDataGrid)dataGrid.FirstOrDefault<UIElement>();
                    myBindableDG.DataSource = ds;
                    myBindableDG.DataMember = "EntityDataTable";
                    myBindableDG.DataBind();
                }
            }
            else
            {
                //Show DataGrid Only when there are records for given search
                if (dt.Rows.Count > 0)
                {
                    StackPanel entityStackPanel = new StackPanel();
                    entityStackPanel.Name = string.Format("StackPanel_{0}", qsfx.EntityLogicalName);

                    #region Entity DataGrid
                    myBindableDG.Name = qsfx.DataGridName;
                    myBindableDG.CRMSourceEntity = qsfx.EntityLogicalName;

                    if (ServerBaseUrl.EndsWith("/"))
                        myBindableDG.ServerBaseUrl = ServerBaseUrl.Substring(0, ServerBaseUrl.Length - 1);
                    else
                        myBindableDG.ServerBaseUrl = ServerBaseUrl;

                    myBindableDG.AutoGenerateColumns = false;
                    myBindableDG.DataSource = ds;
                    myBindableDG.DataMember = "EntityDataTable";
                    myBindableDG.DataBind();
                    #endregion

                    #region Label
                    System.Windows.Controls.Label labelEntityName = new System.Windows.Controls.Label();
                    labelEntityName.Content = XDocument.Parse(qsfx.FormattedFetchXml).Root.Element("entity").Attribute("displayName").Value;
                    labelEntityName.Height = 20;
                    labelEntityName.MinWidth = 1000;
                    labelEntityName.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    labelEntityName.FontWeight = FontWeights.Bold;
                    #endregion

                    #region

                    Image img = new Image();
                    System.Windows.Media.Imaging.BitmapImage imgEntityImg = new System.Windows.Media.Imaging.BitmapImage(new Uri("http://zlcrmweb52/_imgs/ico_16_1.gif")); 
                    img.Source = imgEntityImg;
                    labelEntityName.Height = 16;
                    labelEntityName.Width = 16;
                    #endregion
                        
                    #region Navigation Grid

                    Grid pagerGrid = new Grid();
                    pagerGrid.Name = string.Format("PageGrid_{0}", qsfx.EntityLogicalName);
                    pagerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                    pagerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                    pagerGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

                    #region Next Button
                    Button nextButton = new Button();
                    nextButton.Content = "Next";
                    nextButton.Name = string.Format("NextButton_{0}", qsfx.EntityLogicalName);
                    nextButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    nextButton.Tag = qsfx.EntityLogicalName;
                    nextButton.Click += new RoutedEventHandler(nextButton_Click);

                    pagerGrid.Children.Add(nextButton);
                    Grid.SetColumn(nextButton, 1);
                    //if (qsfx.HasMoreRecords)
                    //{

                    //    nextButton.IsEnabled = true;
                    //}
                    //else
                    //{
                    //    nextButton.IsEnabled = false;
                    //}
                    #endregion

                    #region Prev Button
                    Button prevButton = new Button();
                    prevButton.Content = "Prev";
                    prevButton.Name = string.Format("PrevButton_{0}", qsfx.EntityLogicalName);
                    prevButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    prevButton.Tag = qsfx.EntityLogicalName;
                    prevButton.Click += new RoutedEventHandler(prevButton_Click);

                    //if (qsfx.PageNumber == 1)
                    //{
                    //    prevButton.IsEnabled = false;
                    //}
                    //else
                    //{
                    //    prevButton.IsEnabled = true;
                    //}
                    pagerGrid.Children.Add(prevButton);
                    Grid.SetColumn(prevButton, 0);
                    #endregion

                    #endregion

                    entityStackPanel.Children.Add(new Line());
                    entityStackPanel.Children.Add(labelEntityName);
                    entityStackPanel.Children.Add(img);
                    entityStackPanel.Children.Add(myBindableDG);
                    entityStackPanel.Children.Add(pagerGrid);

                    SearchResultGridPanel.Children.Add(entityStackPanel);
                }
            }

            ControlNavigationButtons(qsfx);
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            //ReportMessage(((Button)sender).Tag.ToString());
            string eln = ((Button)sender).Tag.ToString();

            QuickSearchFetchXml qsfx = this.qsfetchXmlList.Where(a => a.EntityLogicalName == eln).FirstOrDefault<QuickSearchFetchXml>();

            if (qsfx != null)
            {
                ((Button)sender).IsEnabled = false;
                qsfx.PageNumber++;

                GetEntityRecords(qsfx);
            }
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            string eln = ((Button)sender).Tag.ToString();

            QuickSearchFetchXml qsfx = this.qsfetchXmlList.Where(a => a.EntityLogicalName == eln).FirstOrDefault<QuickSearchFetchXml>();

            if (qsfx != null)
            {
                ((Button)sender).IsEnabled = false;
                qsfx.PageNumber--;
                GetEntityRecords(qsfx);
            }
        }

        private void ControlNavigationButtons(QuickSearchFetchXml qsfx)
        {
            string stackPanelName = string.Format("StackPanel_{0}", qsfx.EntityLogicalName);
            string gridName = string.Format("PageGrid_{0}", qsfx.EntityLogicalName);
            string nextButtonName = string.Format("NextButton_{0}", qsfx.EntityLogicalName);
            string prevButtonName = string.Format("PrevButton_{0}", qsfx.EntityLogicalName);

            try
            {
                var stackPanel = ((UIElementCollection)SearchResultGridPanel.Children).Where(a => a.GetType() == typeof(StackPanel) && ((StackPanel)a).Name == stackPanelName).FirstOrDefault<UIElement>();
                if (stackPanel != null)
                {
                    var grid = ((StackPanel)stackPanel).Children.Where(a => a.GetType() == typeof(Grid) && ((Grid)a).Name == gridName).FirstOrDefault<UIElement>();
                    if (grid != null)
                    {
                        var nextButton = ((Grid)grid).Children.Where(a => a.GetType() == typeof(Button) && ((Button)a).Name == nextButtonName).FirstOrDefault<UIElement>();
                        var prevButton = ((Grid)grid).Children.Where(a => a.GetType() == typeof(Button) && ((Button)a).Name == prevButtonName).FirstOrDefault<UIElement>();

                        if (nextButton != null)
                        {
                            if (qsfx.HasMoreRecords)
                            {
                                ((Button)nextButton).IsEnabled = true;
                            }
                            else
                            {
                                ((Button)nextButton).IsEnabled = false;
                            }
                        }

                        if (prevButton != null)
                        {
                            if (qsfx.PageNumber == 1)
                            {
                                ((Button)prevButton).IsEnabled = false;
                            }
                            else
                            {
                                ((Button)prevButton).IsEnabled = true;
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                ReportError(Ex);
            }
        }

        public string CreateXml(string xml, string cookie, int page, int count)
        {
            XDocument doc = XDocument.Parse(xml);

            //StringReader stringReader = new StringReader(xml);
            //XmlTextReader reader = new XmlTextReader(stringReader);

            //// Load document
            //XmlDocument doc = new XmlDocument();
            //doc.Load(reader);
            if (cookie != null && cookie.Trim().Length > 0)
            {
                doc.Root.Add(new XAttribute("paging-cookie", cookie));
            }
            doc.Root.Add(new XAttribute("count", count.ToString()));
            doc.Root.Add(new XAttribute("page", page.ToString()));

            return doc.ToString();
        }

        private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GlobalSearch();
            }
        }
              
    }

    public class QuickSearchFetchXml
    {
        private int _pageNumber = 1;

        public int ObjectTypeCode { get; set; }
        public string EntityLogicalName { get; set; }
        public string EntityDisplayName { get; set; }

        public string FetchXml { get; set; }
        public string FormattedFetchXml { get; set; }
        public string TransformedFetchXml { get; set; }

        public string PagingCookie { get; set; }
        public int PageNumber { 
            get
            {
                return _pageNumber;
            }
            set
            {
                _pageNumber = value;
            }
        }

        public bool HasMoreRecords { get; set; }

        public string DataGridName
        {
            get { return string.Format("dataGrid_{0}", EntityLogicalName); }
        }

        
    }

    public class AsynchMethodParameters
    {
        public IOrganizationService Service { get; set; }
        public QuickSearchFetchXml  QSFetchXml { get; set; }

    }
}
