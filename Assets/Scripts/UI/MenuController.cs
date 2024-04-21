// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using Gameplay;

// namespace UI
// {
//     public class MenuController : MonoBehaviour
//     {
//         [SerializeField] GameObject pauseMenu;
//         [SerializeField] public PlayerInputHandler _inputHandler;

//         public void Update()
//         {
//             bool hasPausedGame = _inputHandler.GetPauseGameInput();
//             if (hasPausedGame)
//             {
//                 Debug.Log("hasPausedGame" + hasPausedGame);
//                 pauseMenu.SetActive(true);
//                 Time.timeScale = 0;
//             }
//         }
//     }
// }
