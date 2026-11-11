/**
 * Bangla texts (the default language). Wording agreed in docs/UI_DESIGN.md §8.
 * `en.ts` must have exactly the same keys; a missing or extra key is a type error.
 */
export const bn = {
  'app.brand': 'ভর্তি',
  'nav.classes': 'ক্লাসসমূহ',
  'nav.myApplications': 'আমার আবেদন',
  'nav.createClass': 'ক্লাস তৈরি (অ্যাডমিন)',
  'nav.menu': 'মেনু',
  'lang.bn': 'বাংলা',
  'lang.en': 'EN',

  'student.label': 'শিক্ষার্থী:',
  'student.enter': 'শিক্ষার্থী আইডি দিন',
  'student.change': 'পরিবর্তন',
  'student.dialogTitle': 'জন্ম নিবন্ধন নম্বর দিন',
  'student.hint': '১৭ সংখ্যা',
  'student.invalid': 'ঠিক ১৭টি সংখ্যা দিন',
  'common.cancel': 'বাতিল',
  'common.close': 'বন্ধ করুন',
  'common.save': 'সংরক্ষণ করুন',

  'page.comingSoon': 'এই পাতাটি শীঘ্রই আসছে।',
  'notFound.title': 'পাতাটি পাওয়া যায়নি।',
  'notFound.back': 'ক্লাস তালিকায় ফিরে যান',
  'crash.title': 'কিছু একটা ভুল হয়েছে।',
  'crash.refresh': 'রিফ্রেশ করুন',

  'error.NO_SEATS_AVAILABLE': 'দুঃখিত, এই ক্লাসে আর কোনো আসন খালি নেই।',
  'error.STUDENT_HAS_ACTIVE_HOLD': 'আপনার ইতিমধ্যে একটি সক্রিয় আবেদন আছে।',
  'error.HOLD_EXPIRED': 'সময় শেষ হয়ে গেছে। আবার আবেদন করুন।',
  'error.HOLD_CANCELLED': 'এই আবেদনটি বাতিল করা হয়েছে।',
  'error.HOLD_ALREADY_CONFIRMED': 'ভর্তি নিশ্চিত হয়েছে, এখন আর বাতিল করা যাবে না।',
  'error.NOT_HOLD_OWNER': 'এই আবেদনটি অন্য শিক্ষার্থীর।',
  'error.CLASS_NOT_FOUND': 'ক্লাসটি পাওয়া যায়নি।',
  'error.HOLD_NOT_FOUND': 'আবেদনটি পাওয়া যায়নি।',
  'error.INVALID_STUDENT_ID': 'জন্ম নিবন্ধন নম্বর ঠিক ১৭ সংখ্যার হতে হবে।',
  'error.VALIDATION_FAILED': 'ফর্মের তথ্য ঠিক করুন।',
  'error.MALFORMED_REQUEST': 'অনুরোধটি সঠিক নয়।',
  'error.INTERNAL_ERROR': 'সার্ভারে সমস্যা হয়েছে। একটু পরে আবার চেষ্টা করুন।',
  'error.NETWORK': 'সার্ভারের সাথে সংযোগ করা যাচ্ছে না। আবার চেষ্টা হচ্ছে…',
} satisfies Record<string, string>

export type MessageKey = keyof typeof bn
export type Dictionary = Record<MessageKey, string>
