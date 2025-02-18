using ExtensionFunctions;
using Model;
using UnityEngine;
using Utils;

namespace Prefabs.Player
{
    public class InputInterface
    {
        private readonly bool _isBot;

        public InputInterface(bool isBot)
        {
            _isBot = isBot;
        }

        private bool _isJumpDown, _isReloadDown, _isCrouchingDown, _isCrouchingUp, _isSprinting;
        private WeaponType? _weaponSelection;
        private Vector2 _axis;

        public Vector2 Axis
        {
            get => _isBot ? _axis : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            set => _axis = value;
        }


        public bool IsPauseDown => !_isBot && Input.GetKeyDown(KeyCode.Escape);
        public bool IsInventoryDown => !_isBot && Input.GetKeyDown(KeyCode.I);

        public bool IsJumpDown
        {
            get => _isBot ? _isJumpDown.GetAsTrigger() : Input.GetButtonDown("Jump");
            set => _isJumpDown = value;
        }

        public bool IsReloadDown
        {
            get => _isBot ? _isReloadDown.GetAsTrigger() : Input.GetKeyDown(KeyCode.R);
            set => _isReloadDown = value;
        }

        public bool IsAimToggleDown => !_isBot && Input.GetMouseButtonDown(1);

        public bool IsSprinting
        {
            get => _isBot ? _isSprinting : Input.GetKey(KeyCode.LeftShift);
            set => _isSprinting = value;
        }

        public bool IsCrouchingDown
        {
            get => _isBot ? _isCrouchingDown.GetAsTrigger() : Input.GetKeyDown(KeyCode.LeftControl);
            set => _isCrouchingDown = value;
        }

        public bool IsCrouchingUp
        {
            get => _isBot ? _isCrouchingUp.GetAsTrigger() : Input.GetKeyUp(KeyCode.LeftControl);
            set => _isCrouchingUp = value;
        }

        public WeaponType? WeaponSelection
        {
            get =>
                _isBot
                    ? _weaponSelection.GetAsTrigger()
                    : (Input.GetKeyDown(KeyCode.Alpha1) ? WeaponType.Block :
                        Input.GetKeyDown(KeyCode.Alpha2) ? WeaponType.Melee :
                        Input.GetKeyDown(KeyCode.Alpha3) ? WeaponType.Primary :
                        Input.GetKeyDown(KeyCode.Alpha4) ? WeaponType.Secondary :
                        Input.GetKeyDown(KeyCode.Q) ? WeaponType.Tertiary : null);
            set => _weaponSelection = value;
        }
    }
}