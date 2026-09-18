# Check-in scanner real-device checklist

Use this checklist on the device and browser that will be used by the check-in crew. It complements the automated component tests, which use a fake decoder; it does not replace a real-device check.

## Before the event

- [ ] Open the Admin UI over **HTTPS**. Camera access requires a secure browser context; do not use an HTTP deployment for this check.
- [ ] Sign in and open the intended team and event's **Check-in** page. Confirm that the event and team are correct.
- [ ] Confirm the device is online and can reach the Admin UI and API.
- [ ] If the browser asks for camera permission, allow it for the Admin UI.
- [ ] Confirm the scanner starts with the device's rear camera selected. Keep the QR code inside the scan frame.
- [ ] Press **Switch camera** and confirm that the front camera starts. Press it again to return to the rear camera.
- [ ] If using a keyboard-wedge QR scanner, connect it and configure it to send the decoded value followed by **Enter**. Leave focus outside a text field, scan a test code, and confirm that no manual confirmation is needed.
- [ ] Check the sound control. Confirm **Mute sound** silences the scanner, and **Unmute sound** restores feedback.

## Verify scan outcomes

Use known test registrations where possible. Confirm the message and sound before moving to the next test.

- [ ] **Successful QR scan:** the attendee and ticket are shown, the success sound is distinct from an error, the camera pauses, and the screen says **Ready for the next scan**. After about two seconds it is ready for the next ticket and the check-in count refreshes.
- [ ] **Duplicate:** scan an already checked-in registration. Confirm the screen says **Already checked in** and includes the recorded check-in time when available. It must not create another check-in.
- [ ] **Cancelled:** scan a cancelled registration. Confirm the screen says **Cancelled — Create Registration is required** and offers the registration list; do not treat it as checked in.
- [ ] **Invalid:** scan a QR value that is missing, malformed, or belongs to another event. Confirm the screen says **This credential is not valid for this event**.
- [ ] **Network retry:** disable connectivity, scan a valid test code, and confirm the network error says the credential is retained. Restore connectivity, select **Retry**, and confirm the same credential is submitted and the successful result appears.
- [ ] **Early arrival:** during the 30 minutes before the event start, confirm the start-time warning appears. Select **Got it** and confirm it is dismissed while the scanner remains available. Check-in is still subject to the event being Active; the warning acknowledgement does not change that requirement.
- [ ] Repeat one successful and one unsuccessful scan with sound muted, then unmuted. Confirm that the on-screen outcome remains clear and that the two audible outcomes remain distinct when sound is enabled.

If any check fails, stop using the device for live check-in and record the device, browser, event, network, and observed outcome.
