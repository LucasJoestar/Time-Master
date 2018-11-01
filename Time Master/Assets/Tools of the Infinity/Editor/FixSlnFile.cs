using System.Diagnostics;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class FixSlnFile
{
    /* FixSlnFile :
	*
	* Fix the problem encounter when creating a new script
    * --> MonoBehaviour is sometimes not recognized,
    * 'cause the .sln file of the Project is obsolete...
    * Initially, we need to close the script editor software, delete the .sln file and then regenerate the it.
    * 
    * 
    * To fix this problem, we need to :
    *       - Close the script editor software,
    *       - Delete the .sln file of the Unity Project
    *       - Re-generate this .sln file
    *       - Open again the script editor software...
    * 
    * That's a long process, so let's do all of it with a cool tool !
    * 
    * /!\ For now, the only available method do the job with Visual Studio
	*/

    #region Methods
    /// <summary>
    /// Fix the problem of the obsolete .sln file of the Project
    /// with Visual Studio as script editor software
    /// </summary>
    [MenuItem("Lucas's Cools Tools/Fixs, Shortcuts and Others.../Sln file Fix (Visual Studio)")]
    static void SlnFix_VisualStudio()
    {
        /* Path :
         * 
         * Get the informations related to the Path of the project ;
         * We need :
         *      - The Data Path --> Data Path of the Project, in the Asset folder
         *      - The Project Path --> Full Path of the project, parent of the Asset folder
         *      - Project Name --> Name of the Project
         * And so we get the Path of the .sln File
         *      --> ProjectPath/ProjectName.sln
        */
        string _dataPath = Application.dataPath;
        DirectoryInfo _projectPathInfos = Directory.GetParent(_dataPath);
        string _projectPath = _projectPathInfos.FullName;
        string _projectName = _projectPathInfos.Name;
        string _slnFilePath = Path.Combine(_projectPath, _projectName) + ".sln";

        /* Close Visual :
         * 
         * Now, we need to get all the Processes of Visual Studio,
         * by the "devenv" keyword,
         * and then close them !
        */
        Process[] _visualProcesses = Process.GetProcessesByName("devenv");
        foreach (Process _process in _visualProcesses)
        {
            _process.CloseMainWindow();
            _process.Close();
        }

        /* Destroy .sln :
         * 
         * Visual Studio is closed, and so we can delete the .sln file of the Project
         * If the Project folder or the .sln file cannot be found, Debug it and return
        */
        if(Directory.Exists(_projectPath) && File.Exists(_slnFilePath))
        {
            File.Delete(_slnFilePath);
        }
        else
        {
            Debug.LogWarning("Project Folder or .sln File cannot be found ! The .sln file could not be deleted !");
            return;
        }

        /* Revive Visual Studio :
         * 
         * To finish, we want to re-generate the .sln file of the Project,
         * to do so, let's use EditorApplication and then execute an Item from the Menu,
         * now select to open the c# project to generate the .sln and start Visual Studio !
         * 
         * When this is done, find the latest script created and start it !
        */
        EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        string[] _projectScripts = Directory.GetFiles(_dataPath, "*.cs", SearchOption.AllDirectories);
        if (_projectScripts.Length > 0)
        {
            string _newScript = _projectScripts.OrderByDescending(s => File.GetCreationTime(s)).First();
            Process.Start(_newScript);
        }

        /* That's it, the Fix has been applied !
         * Yeeaaaaah !!
        */
    }
    #endregion
}
