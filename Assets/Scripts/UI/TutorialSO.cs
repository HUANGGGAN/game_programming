using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Game/Tutorial Progress")]
public class TutorialSO : ScriptableObject
{
    [SerializeField] private List<string> readTutorials = new List<string>();
    public bool tutorialsSkipped;
    public bool HasRead(string id) => readTutorials.Contains(id);
    public void MarkRead(string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && !readTutorials.Contains(id)) readTutorials.Add(id);
    }
    public void ResetProgress()
    {
        readTutorials.Clear();
        tutorialsSkipped = false;
    }
}
