using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace UI
{
    public class CrosshairManager : MonoBehaviour
    {
        public Image CrosshairImage;
        public Sprite NullCrosshairSprite;
        public float CrosshairUpdateshrpness = 5f;

        PlayerWeaponsManager _weaponsManager;
        bool _wasPointingAtEnemy;
        RectTransform _crosshairRectTransform;
        CrosshairData _crosshairDataDefault;
        CrosshairData _crosshairDataTarget;
        CrosshairData _currentCrosshair;

        void Start()
        {
            _weaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, CrosshairManager>(_weaponsManager, this);

            OnWeaponChanged(_weaponsManager.GetActiveWeapon());

            _weaponsManager.OnSwitchedToWeapon += OnWeaponChanged;
        }

        void Update()
        {
            UpdateCrosshairPointingAtEnemy(false);
            _wasPointingAtEnemy = _weaponsManager.IsPointingAtEnemy;
        }

        void UpdateCrosshairPointingAtEnemy(bool force)
        {
            if (_crosshairDataDefault.CrosshairSprite == null)
                return;

            if ((force || !_wasPointingAtEnemy) && _weaponsManager.IsPointingAtEnemy)
            {
                _currentCrosshair = _crosshairDataTarget;
                CrosshairImage.sprite = _currentCrosshair.CrosshairSprite;
                _crosshairRectTransform.sizeDelta = _currentCrosshair.CrosshairSize * Vector2.one;
            }
            else if ((force || _wasPointingAtEnemy) && !_weaponsManager.IsPointingAtEnemy)
            {
                _currentCrosshair = _crosshairDataDefault;
                CrosshairImage.sprite = _currentCrosshair.CrosshairSprite;
                _crosshairRectTransform.sizeDelta = _currentCrosshair.CrosshairSize * Vector2.one;
            }

            // CrosshairImage.color = Color.Lerp(CrosshairImage.color, _currentCrosshair.CrosshairColor,
            //     Time.deltaTime * CrosshairUpdateshrpness);

            _crosshairRectTransform.sizeDelta = Mathf.Lerp(_crosshairRectTransform.sizeDelta.x,
                _currentCrosshair.CrosshairSize,
                Time.deltaTime * CrosshairUpdateshrpness) * Vector2.one;
        }

        void OnWeaponChanged(WeaponController newWeapon)
        {
            if (newWeapon)
            {
                CrosshairImage.enabled = true;
                _crosshairDataDefault = newWeapon.CrosshairDataDefault;
                _crosshairDataTarget = newWeapon.CrosshairDataTargetInSight;
                _crosshairRectTransform = CrosshairImage.GetComponent<RectTransform>();
                DebugUtility.HandleErrorIfNullGetComponent<RectTransform, CrosshairManager>(_crosshairRectTransform,
                    this, CrosshairImage.gameObject);
            }
            else
            {
                if (NullCrosshairSprite)
                {
                    CrosshairImage.sprite = NullCrosshairSprite;
                }
                else
                {
                    CrosshairImage.enabled = false;
                }
            }

            UpdateCrosshairPointingAtEnemy(true);
        }
    }
}
