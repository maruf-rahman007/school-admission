import type { Dictionary } from './bn'

/** English texts. Typed against the Bangla keys, so both dictionaries always stay complete. */
export const en: Dictionary = {
  'app.brand': 'Admission',
  'nav.classes': 'Classes',
  'nav.myApplications': 'My applications',
  'nav.createClass': 'Create class (admin)',
  'nav.menu': 'Menu',
  'lang.bn': 'বাংলা',
  'lang.en': 'EN',

  'student.label': 'Student:',
  'student.enter': 'Enter student ID',
  'student.change': 'Change',
  'student.dialogTitle': 'Enter birth registration number',
  'student.hint': '17 digits',
  'student.invalid': 'Enter exactly 17 digits',
  'common.cancel': 'Cancel',
  'common.close': 'Close',
  'common.save': 'Save',

  'page.comingSoon': 'This page is coming soon.',
  'notFound.title': 'Page not found.',
  'notFound.back': 'Back to classes',
  'crash.title': 'Something went wrong.',
  'crash.refresh': 'Refresh',

  'error.NO_SEATS_AVAILABLE': 'Sorry, no seats are left in this class.',
  'error.STUDENT_HAS_ACTIVE_HOLD': 'You already have an active application.',
  'error.HOLD_EXPIRED': 'Time ran out. Please apply again.',
  'error.HOLD_CANCELLED': 'This application was cancelled.',
  'error.HOLD_ALREADY_CONFIRMED': 'Admission is confirmed and can no longer be cancelled.',
  'error.NOT_HOLD_OWNER': 'This application belongs to another student.',
  'error.CLASS_NOT_FOUND': 'Class not found.',
  'error.HOLD_NOT_FOUND': 'Application not found.',
  'error.INVALID_STUDENT_ID': 'Birth registration number must be exactly 17 digits.',
  'error.VALIDATION_FAILED': 'Please fix the form.',
  'error.MALFORMED_REQUEST': 'The request was invalid.',
  'error.INTERNAL_ERROR': 'Something went wrong on the server. Please try again.',
  'error.NETWORK': "Can't reach the server. Retrying…",
}
