using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        public void PlayGame()
        {
            SceneManager.LoadSceneAsync(1);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
