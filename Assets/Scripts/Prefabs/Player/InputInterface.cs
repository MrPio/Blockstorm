using Model;
using UnityEngine;

namespace Prefabs.Player
{
    public class InputInterface
    {
        private bool _isBot;

        public InputInterface(bool isBot)
        {
            _isBot = isBot;
        }

        public Vector2 Axis =>
            _isBot ? Vector2.zero : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        public bool IsPauseDown => !_isBot && Input.GetKeyDown(KeyCode.Escape);
        public bool IsInventoryDown => !_isBot && Input.GetKeyDown(KeyCode.I);
        public bool IsJumpDown => _isBot ? false : Input.GetButtonDown("Jump");
        public bool IsReloadDown => _isBot ? false : Input.GetKeyDown(KeyCode.R);
        public bool IsAimToggleDown => _isBot ? false : Input.GetMouseButtonDown(1);
        public bool IsSprinting => _isBot ? false : Input.GetKey(KeyCode.LeftShift); //non usare as trigger qui
        public bool IsCrouchingDown => _isBot ? false : Input.GetKeyDown(KeyCode.LeftControl);
        public bool IsCrouchingUp => _isBot ? false : Input.GetKeyUp(KeyCode.LeftControl); 

        public WeaponType? WeaponSelection =>
            _isBot
                ? null
                : (Input.GetKeyDown(KeyCode.Alpha1) ? WeaponType.Block :
                    Input.GetKeyDown(KeyCode.Alpha2) ? WeaponType.Melee :
                    Input.GetKeyDown(KeyCode.Alpha3) ? WeaponType.Primary :
                    Input.GetKeyDown(KeyCode.Alpha4) ? WeaponType.Secondary :
                    Input.GetKeyDown(KeyCode.Q) ? WeaponType.Tertiary : null);
    }
}