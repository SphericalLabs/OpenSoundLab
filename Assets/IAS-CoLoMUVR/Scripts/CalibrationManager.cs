//The following code is licensed by CC BY 4.0
//at the Immersive Arts Space, Zurich University of the Arts
//By Chris Elvis Leisi - Associate Researcher at the Immersive Arts Space

using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IAS.CoLocationMUVR
{
    public class CalibrationManager : MonoBehaviour
	{
		public static CalibrationManager Instance;
		private OVRCameraRig _targetPlayer;
        private Transform trackingCenter;
        private Transform playerHead;
        private Transform playerLeftHand;
        private Transform playerRightHand;
        private Transform calculationTarget;
		
		
		private bool isCalibrationActive = false;
		public GameObject uiParentObject;
		public GameObject calibrateCourser;

		public float minAngleOffset = 15f;
		public float pressWaitingTime = 3f;
		public Image pressWaitingImage;
		private bool buttonsArePressd = false;
		private float startPressTime;
		private bool hasCenterCalibrated = false;


		private void Awake()
		{
			SetTargetPlayer();
			ToggleCalibrationUI(false);
        }

		public void SetTargetPlayer()
		{
			_targetPlayer = FindObjectOfType<OVRCameraRig>();

			if (_targetPlayer != null)
            {
				trackingCenter = _targetPlayer.trackingSpace;
                playerHead = _targetPlayer.centerEyeAnchor;
                playerRightHand = _targetPlayer.rightHandAnchor;
                playerLeftHand = _targetPlayer.leftHandAnchor;
            }
        }
		
		private void Update()
		{
			//FollowPlayer();

			//toggle on/off UI
			if (Input.GetKeyDown(KeyCode.P) || OVRInput.GetDown(OVRInput.Button.Start))
            {
                ToggleCalibrationUI(!isCalibrationActive);
            }

            //if calibration mode is active, update courser oritentation
            if (calibrateCourser.activeInHierarchy)
				SetCalibrationCourser();
		}

		//follow the player head
		void FollowPlayer()
		{
			transform.position = playerHead.position;
			Quaternion newRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerHead.forward, transform.up));
			if (Quaternion.Angle(transform.rotation, newRotation) > minAngleOffset)
				transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, 1.5f * Time.deltaTime);
			
		}
		

		//activat/deactivate calibration UI and courser
		public void ToggleCalibrationUI(bool b)
		{
			if (uiParentObject != null)
				uiParentObject.SetActive(b);
			if (calibrateCourser != null)
				calibrateCourser.SetActive(b);

			isCalibrationActive = b;
		}


		//update courser oroentation to player hands
		void SetCalibrationCourser()
		{
			this.calibrateCourser.transform.position = this.playerRightHand.position;
			Vector3 yNeutralPos = this.playerLeftHand.position;
			yNeutralPos.y = this.playerRightHand.transform.position.y;
			
			if (this.playerHead.parent != null)
				this.calibrateCourser.transform.transform.LookAt(yNeutralPos, this.playerHead.parent.transform.up);
			
			//after calculate rotation set to right hand position
			this.calibrateCourser.transform.position = this.playerRightHand.position;

			//Trigger buttons
			if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) > 0.5f && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) > 0.5f)
			{
				if (!buttonsArePressd)
				{
					buttonsArePressd = true;
					startPressTime = Time.time;
				}
				else
				{
					if (pressWaitingImage != null)
						pressWaitingImage.fillAmount = (Time.time - startPressTime) / pressWaitingTime;
					if (!hasCenterCalibrated && buttonsArePressd && startPressTime + pressWaitingTime < Time.time)
					{
						//CenterCalibrator.Instance.CalibrateCenter(this.replaceObjects);
						CalibrateCenter();
						hasCenterCalibrated = true;
						if (pressWaitingImage != null)
							pressWaitingImage.fillAmount = 0f;
					}
				}
			}
			else
			{
				if (pressWaitingImage != null && this.pressWaitingImage.fillAmount > 0)
					pressWaitingImage.fillAmount = 0f;
				hasCenterCalibrated = false;
				buttonsArePressd = false;
			}
		}


        void CalibrateCenter()
        {
            Vector3 yNeutralPos;

            trackingCenter.localPosition = Vector3.zero;
            trackingCenter.localRotation = Quaternion.identity;

            if (calculationTarget == null)
            {
                calculationTarget = new GameObject().transform;
                calculationTarget.parent = trackingCenter.parent;
				calculationTarget.gameObject.name = "CalibrationTarget";
            }

            //set rotation
            calculationTarget.position = playerRightHand.transform.position;
            yNeutralPos = playerLeftHand.transform.position;
            yNeutralPos.y = playerRightHand.transform.position.y;
            calculationTarget.transform.LookAt(yNeutralPos, trackingCenter.up);

            //after calculate rotation set to right hand position
            calculationTarget.position = playerRightHand.transform.position;

            //set offset
            trackingCenter.localPosition = calculationTarget.InverseTransformPoint(Vector3.zero);
            trackingCenter.localRotation = Quaternion.Inverse(calculationTarget.rotation);

            Debug.Log("Calibrate center");
        }
    }
}
