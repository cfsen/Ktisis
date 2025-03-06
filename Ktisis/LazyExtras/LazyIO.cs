using Dalamud.Interface.ImGuiFileDialog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Lumina.Excel.Sheets;

namespace Ktisis.LazyExtras;
class LazyIO {
	FileDialogManager fdm;
	public LazyIO() { 
		fdm = new();
	}

	// ImGui

	private void OpenFileDialog(string title, string filters, Action<bool, List<string>> callback) {
		fdm.OpenFileDialog(title, filters, callback, 1);
	}
	private void OpenDirDialog(string title, string filters, Action<bool, List<string>> callback) {

	}

	// Quick access 

	private void SetupQuickAx() {

	}

	private void GetQuickAxDirs() {
		
	}
	private void SaveQuickAxDirs() {

	}
}
