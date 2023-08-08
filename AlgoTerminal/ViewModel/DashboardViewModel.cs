using System;
using System.Windows;
using System.Xml.Linq;
using System.ComponentModel;
using AlgoTerminal.View;
using System.IO;

namespace AlgoTerminal.ViewModel
{
    public class DashboardViewModel:BaseViewModel
    {
        private Sample1 sample1;
        private Sample2 sample2;
        static string SettingFileName { get { return string.Format(@"{0}\{1}", Environment.CurrentDirectory, "Layout.xml"); } }
        public DashboardViewModel(Sample2 sample2, Sample1 sample1)
        {
            this.sample2 = sample2;
            this.sample1 = sample1;

            DockInitialization();
        }

        private void DockInitialization()
        {
            DashboardView.dockManager.RegisterDock(this.sample1);
            DashboardView.dockManager.RegisterDock(this.sample2);
        }

        public void DashboardView_Closing(object? sender, CancelEventArgs e)
        {
            DashboardView.dockManager.SaveCurrentLayout("DashboardView");

            var doc = new XDocument();
            var rootNode = new XElement("Layouts");
            foreach (var layout in DashboardView.dockManager.Layouts.Values)
                layout.Save(rootNode);
            doc.Add(rootNode);
            doc.Save(SettingFileName);
            DashboardView.dockManager.Dispose();
        }

        public void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(SettingFileName))
            {
                var layout = XDocument.Parse(File.ReadAllText(SettingFileName));
                foreach (var item in layout.Root.Elements())
                {
                    var name = item.Attribute("Name").Value;
                    if (DashboardView.dockManager.Layouts.ContainsKey(name))
                        DashboardView.dockManager.Layouts[name].Load(item);
                    else DashboardView.dockManager.Layouts[name] = new VishwaDockLibNew.LayoutSetting.LayoutSetting(name, item);
                }

                DashboardView.dockManager.ApplyLayout("DashboardView");
            }
            else
            {
                this.sample1.DockControl.Show();
                this.sample2.DockControl.Show();
            }
        }
    }
}
