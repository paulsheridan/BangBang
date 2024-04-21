using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

using ECM2;

namespace Gameplay
{
    public class PromptManager : MonoBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI promptText;

        private PlayerInteract _playerInteract;

        void Start()
        {
            PlayerCharacter playerCharacter =
                GameObject.FindObjectOfType<PlayerCharacter>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerCharacter, PromptManager>(
                playerCharacter, this);

            _playerInteract = playerCharacter.GetComponent<PlayerInteract>();
            DebugUtility.HandleErrorIfNullGetComponent<PromptManager, PlayerInteract>(_playerInteract, this,
                playerCharacter.gameObject);

            _playerInteract.UpdateText += UpdateText;
        }

        void UpdateText(string promptMessage)
        {
            promptText.text = promptMessage;
        }
    }
}
