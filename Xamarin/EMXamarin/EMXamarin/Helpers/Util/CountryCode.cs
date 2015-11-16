using System.Collections.Generic;

namespace em {
	public class CountryCode {

		public static Dictionary<string, CountryCode> countryMapByCode;
		public static Dictionary<string, CountryCode> countryMapByName;

		public string countryName { get; set; }
		public string countryCode { get; set; }
		public string phonePrefix { get; set; }
		public string photoUrl { get; set; }
		public string translationKey { get; set; }

		public CountryCode (string countryName, string countryCode, string phonePrefix, string photoUrl, string translationKey) {
			this.countryName = countryName;
			this.countryCode = countryCode;
			this.phonePrefix = phonePrefix;
			this.photoUrl = photoUrl;
			this.translationKey = translationKey;
		}

		public static IList<CountryCode> countries = new List<CountryCode> { 
			new CountryCode("Afghanistan", "af", "93", "af.png", "AFGHANISTAN"),
			new CountryCode("Albania", "al", "355", "al.png", "ALBANIA"),
			new CountryCode("Algeria", "dz", "213", "dz.png", "ALGERIA"),
			new CountryCode("American Samoa", "as", "1 684", "as.png", "AMERICAN_SAMOA"),
			new CountryCode("Andorra", "ad", "376", "ad.png", "ANDORRA"),
			new CountryCode("Angola", "ao", "244", "ao.png", "ANGOLA"),
			new CountryCode("Anguilla", "ai", "1 264", "gb.png", "ANGUILLA"), //apply UK flag
			//new CountryCode("Antarctica", "aq", "672", "aq.png", ""), //not needed
			new CountryCode("Antigua and Barbuda", "ag", "1 268", "ag.png", "ANTIGUA_AND_BARBUDA"),
			new CountryCode("Argentina", "ar", "54", "ar.png", "ARGENTINA"),
			new CountryCode("Armenia", "am", "374", "am.png", "ARMENIA"),
			new CountryCode("Aruba", "aw", "297", "aw.png", "ARUBA"),
			new CountryCode("Australia", "au", "61", "au.png", "AUSTRALIA"),
			new CountryCode("Austria", "at", "43", "at.png", "AUSTRIA"),
			new CountryCode("Azerbaijan", "az", "994", "az.png", "AZERBAIJAN"),
			new CountryCode("Bahamas", "bs", "1 242", "bs.png", "BAHAMAS"),
			new CountryCode("Bahrain", "bh", "973", "bh.png", "BAHRAIN"),
			new CountryCode("Bangladesh", "bd", "880", "bd.png", "BANGLADESH"),
			new CountryCode("Barbados", "bb", "1 246", "bb.png", "BARBADOS"),
			new CountryCode("Belarus", "by", "375", "by.png", "BELARUS"),
			new CountryCode("Belgium", "be", "32", "be.png", "BELGIUM"),
			new CountryCode("Belize", "bz", "501", "bz.png", "BELIZE"),
			new CountryCode("Benin", "bj", "229", "bj.png", "BENIN"),
			new CountryCode("Bermuda", "bm", "1 441", "bm.png", "BERMUDA"),
			new CountryCode("Bhutan", "bt", "975", "bt.png", "BHUTAN"),
			new CountryCode("Bolivia", "bo", "591", "bo.png", "BOLIVIA"),
			new CountryCode("Bosnia and Herzegovina", "ba", "387", "ba.png", "BOSNIA_AND_HERZEGOVINA"),
			new CountryCode("Botswana", "bw", "267", "bw.png", "BOTSWANA"),
			new CountryCode("Brazil", "br", "55", "br.png", "BRAZIL"),
			new CountryCode("British Virgin Islands", "vg", "1 284", "gb.png", "BRITISH_VIRGIN_ISLANDS"), //apply UK flag
			new CountryCode("Brunei", "bn", "673", "bn.png", "BRUNEI"),
			new CountryCode("Bulgaria", "bg", "359", "bg.png", "BULGARIA"),
			new CountryCode("Burkina Faso", "bf", "226", "bf.png", "BURKINA_FASO"),
			new CountryCode("Burma (Myanmar)", "mm", "95", "mm.png", "BURMA"),
			new CountryCode("Burundi", "bi", "257", "bi.png", "BURUNDI"),
			new CountryCode("Cambodia", "kh", "855", "kh.png", "CAMBODIA"),
			new CountryCode("Cameroon", "cm", "237", "cm.png", "CAMEROON"),
			new CountryCode("Canada", "ca", "1", "ca.png", "CANADA"),
			new CountryCode("Cape Verde", "cv", "238", "cv.png", "CAPE_VERDE"),
			new CountryCode("Cayman Islands", "ky", "1 345", "ky.png", "CAYMAN_ISLANDS"),
			new CountryCode("Central African Republic", "cf", "236", "cf.png", "CENTRAL_AFRICAN_REPUBLIC"),
			new CountryCode("Chad", "td", "235", "td.png", "CHAD"),
			new CountryCode("Chile", "cl", "56", "cl.png", "CHILE"),
			new CountryCode("China", "cn", "86", "cn.png", "CHINA"),
			//new CountryCode("Christmas Island", "cx", "61", "cx.png", ""), //not needed
			//new CountryCode("Cocos (Keeling) Islands", "cc", "61", "cc.png", ""), //not needed
			new CountryCode("Colombia", "co", "57", "co.png", "COLOMBIA"),
			new CountryCode("Comoros", "km", "269", "km.png", "COMOROS"),
			new CountryCode("Cook Islands", "ck", "682", "ck.png", "COOK_ISLANDS"),
			new CountryCode("Costa Rica", "cr", "506", "cr.png", "COSTA_RICA"),
			new CountryCode("Croatia", "hr", "385", "hr.png", "CROATIA"),
			new CountryCode("Cuba", "cu", "53", "cu.png", "CUBA"),
			new CountryCode("Cyprus", "cy", "357", "cy.png", "CYPRUS"),
			new CountryCode("Czech Republic", "cz", "420", "cz.png", "CZECH_REPUBLIC"),
			new CountryCode("Democratic Republic of the Congo", "cd", "243", "cd.png", "CONGO"),
			new CountryCode("Denmark", "dk", "45", "dk.png", "DENMARK"),
			new CountryCode("Djibouti", "dj", "253", "dj.png", "DJIBOUTI"),
			new CountryCode("Dominica", "dm", "1 767", "dm.png", "DOMINICA"),
			new CountryCode("Dominican Republic", "do", "1 809", "do_.png", "DOMINICAN_REPUBLIC"),
			new CountryCode("Ecuador", "ec", "593", "ec.png", "ECUADOR"),
			new CountryCode("Egypt", "eg", "20", "eg.png", "EGYPT"),
			new CountryCode("El Salvador", "sv", "503", "sv.png", "EL_SALVADOR"),
			new CountryCode("Equatorial Guinea", "gq", "240", "gq.png", "EQUATORIAL_GUINEA"),
			new CountryCode("Eritrea", "er", "291", "er.png", "ERITREA"),
			new CountryCode("Estonia", "ee", "372", "ee.png", "ESTONIA"),
			new CountryCode("Ethiopia", "et", "251", "et.png", "ETHIOPIA"),
			new CountryCode("Falkland Islands", "fk", "500", "gb.png", "FALKLAND_ISLANDS"), //apply UK flag
			new CountryCode("Faroe Islands", "fo", "298", "dk.png", "FAROE_ISLANDS"), //apply Denmark (danish) flag
			new CountryCode("Fiji", "fj", "679", "fj.png", "FIJI"),
			new CountryCode("Finland", "fi", "358", "fi.png", "FINLAND"),
			new CountryCode("France", "fr", "33", "fr.png", "FRANCE"),
			new CountryCode("French Polynesia", "pf", "689", "pf.png", "FRENCH_POLYNESIA"),
			new CountryCode("Gabon", "ga", "241", "ga.png", "GABON"),
			new CountryCode("Gambia", "gm", "220", "gm.png", "GAMBIA"),
			new CountryCode("Georgia", "ge", "995", "ge.png", "GEORGIA"),
			new CountryCode("Germany", "de", "49", "de.png", "GERMANY"),
			new CountryCode("Ghana", "gh", "233", "gh.png", "GHANA"),
			new CountryCode("Gibraltar", "gi", "350", "gb.png", "GIBRALTAR"), //apply UK flag
			new CountryCode("Greece", "gr", "30", "gr.png", "GREECE"),
			new CountryCode("Greenland", "gl", "299", "dk.png", "GREENLAND"), //apply Denmark (danish) flag
			new CountryCode("Grenada", "gd", "1 473", "gd.png", "GRENADA"),
			new CountryCode("Guam", "gu", "1 671", "gu.png", "GUAM"),
			new CountryCode("Guatemala", "gt", "502", "gt.png", "GUATEMALA"),
			new CountryCode("Guinea", "gn", "224", "gn.png", "GUINEA"),
			new CountryCode("Guinea-Bissau", "gw", "245", "gw.png", "GUINEA_BISSAU"),
			new CountryCode("Guyana", "gy", "592", "gy.png", "GUYANA"),
			new CountryCode("Haiti", "ht", "509", "ht.png", "HAITI"),
			new CountryCode("Holy See (Vatican City)", "va", "39", "va.png", "HOLY_SEE_VATICAN_CITY"),
			new CountryCode("Honduras", "hn", "504", "hn.png", "HONDURAS"),
			new CountryCode("Hong Kong", "hk", "852", "hk.png", "HONG_KONG"),
			new CountryCode("Hungary", "hu", "36", "hu.png", "HUNGARY"),
			new CountryCode("Iceland", "is", "354", "is.png", "ICELAND"),
			new CountryCode("India", "in", "91", "in.png", "INDIA"),
			new CountryCode("Indonesia", "id", "62", "id.png", "INDONESIA"),
			new CountryCode("Iran", "ir", "98", "ir.png", "IRAN"),
			new CountryCode("Iraq", "iq", "964", "iq.png", "IRAQ"),
			new CountryCode("Ireland", "ie", "353", "ie.png", "IRELAND"),
			new CountryCode("Isle of Man", "im", "44", "gb.png", "ISLE_OF_MAN"), //apply UK flag
			new CountryCode("Israel", "il", "972", "il.png", "ISRAEL"),
			new CountryCode("Italy", "it", "39", "it.png", "ITALY"),
			new CountryCode("Ivory Coast", "ci", "225", "ci.png", "IVORY_COAST"),
			new CountryCode("Jamaica", "jm", "1 876", "jm.png", "JAMAICA"),
			new CountryCode("Japan", "jp", "81", "jp.png", "JAPAN"),
			new CountryCode("Jordan", "jo", "962", "jo.png", "JORDAN"),
			new CountryCode("Kazakhstan", "kz", "7", "kz.png", "KAZAKHSTAN"),
			new CountryCode("Kenya", "ke", "254", "ke.png", "KENYA"),
			new CountryCode("Kiribati", "ki", "686", "ki.png", "KIRIBATI"),
			new CountryCode("Kosovo", "kv", "381", "kv.png", "KOSOVO"),
			new CountryCode("Kuwait", "kw", "965", "kw.png", "KUWAIT"),
			new CountryCode("Kyrgyzstan", "kg", "996", "kg.png", "KYRGYZSTAN"),
			new CountryCode("Laos", "la", "856", "la.png", "LAOS"),
			new CountryCode("Latvia", "lv", "371", "lv.png", "LATVIA"),
			new CountryCode("Lebanon", "lb", "961", "lb.png", "LEBANON"),
			new CountryCode("Lesotho", "ls", "266", "ls.png", "LESOTHO"),
			new CountryCode("Liberia", "lr", "231", "lr.png", "LIBERIA"),
			new CountryCode("Libya", "ly", "218", "ly.png", "LIBYA"),
			new CountryCode("Liechtenstein", "li", "423", "li.png", "LIECHTENSTEIN"),
			new CountryCode("Lithuania", "lt", "370", "lt.png", "LITHUANIA"),
			new CountryCode("Luxembourg", "lu", "352", "lu.png", "LUXEMBOURG"),
			new CountryCode("Macau", "mo", "853", "mo.png", "MACAU"),
			new CountryCode("Macedonia", "mk", "389", "mk.png", "MACEDONIA"),
			new CountryCode("Madagascar", "mg", "261", "mg.png", "MADAGASCAR"),
			new CountryCode("Malawi", "mw", "265", "mw.png", "MALAWI"),
			new CountryCode("Malaysia", "my", "60", "my.png", "MALAYSIA"),
			new CountryCode("Maldives", "mv", "960", "mv.png", "MALDIVES"),
			new CountryCode("Mali", "ml", "223", "ml.png", "MALI"),
			new CountryCode("Malta", "mt", "356", "mt.png", "MALTA"),
			new CountryCode("Marshall Islands", "mh", "692", "mh.png", "MARSHALL_ISLANDS"),
			new CountryCode("Mauritania", "mr", "222", "mr.png", "MAURITANIA"),
			new CountryCode("Mauritius", "mu", "230", "mu.png", "MAURITIUS"),
			new CountryCode("Mayotte", "yt", "262", "fr.png", "MAYOTTE"), //apply FR (French) flag
			new CountryCode("Mexico", "mx", "52", "mx.png", "MEXICO"),
			new CountryCode("Micronesia", "fm", "691", "fm.png", "MICRONESIA"),
			new CountryCode("Moldova", "md", "373", "md.png", "MOLDOVA"),
			new CountryCode("Monaco", "mc", "377", "mc.png", "MONACO"),
			new CountryCode("Mongolia", "mn", "976", "mn.png", "MONGOLIA"),
			new CountryCode("Montenegro", "me", "382", "me.png", "MONTENEGRO"),
			new CountryCode("Montserrat", "ms", "1 664", "gb.png", "MONTSERRAT"), //apply UK flag
			new CountryCode("Morocco", "ma", "212", "ma.png", "MOROCCO"),
			new CountryCode("Mozambique", "mz", "258", "mz.png", "MOZAMBIQUE"),
			new CountryCode("Namibia", "na", "264", "na.png", "NAMIBIA"),
			new CountryCode("Nauru", "nr", "674", "nr.png", "NAURU"),
			new CountryCode("Nepal", "np", "977", "np.png", "NEPAL"),
			new CountryCode("Netherlands", "nl", "31", "nl.png", "NETHERLANDS"),
			//new CountryCode("Netherlands Antilles", "an", "599", "an.png", ""), //not needed
			new CountryCode("New Caledonia", "nc", "687", "fr.png", "NEW_CALEDONIA"), //apply FR (French) flag
			new CountryCode("New Zealand", "nz", "64", "nz.png", "NEW_ZEALAND"),
			new CountryCode("Nicaragua", "ni", "505", "ni.png", "NICARAGUA"),
			new CountryCode("Niger", "ne", "227", "ne.png", "NIGER"),
			new CountryCode("Nigeria", "ng", "234", "ng.png", "NIGERIA"),
			new CountryCode("Niue", "nu", "683", "nu.png", "NIUE"),
			new CountryCode("Norfolk Island", "nfk", "672", "nfk.png", "NORFOLK_ISLAND"), //3 digit country code
			new CountryCode("North Korea", "kp", "850", "kp.png", "NORTH_KOREA"),
			new CountryCode("Northern Mariana Islands", "mp", "1 670", "us.png", "NORTHERN_MARIANA_ISLANDS"), //apply US flag
			new CountryCode("Norway", "no", "47", "no.png", "NORWAY"),
			new CountryCode("Oman", "om", "968", "om.png", "OMAN"),
			new CountryCode("Pakistan", "pk", "92", "pk.png", "PAKISTAN"),
			new CountryCode("Palau", "pw", "680", "pw.png", "PALAU"),
			new CountryCode("Panama", "pa", "507", "pa.png", "PANAMA"),
			new CountryCode("Papua New Guinea", "pg", "675", "pg.png", "PAPUA_NEW_GUINEA"),
			new CountryCode("Paraguay", "py", "595", "py.png", "PARAGUAY"),
			new CountryCode("Peru", "pe", "51", "pe.png", "PERU"),
			new CountryCode("Philippines", "ph", "63", "ph.png", "PHILIPPINES"),
			new CountryCode("Pitcairn Islands", "pn", "870", "gb.png", "PITCAIRN_ISLANDS"), //apply UK flag
			new CountryCode("Poland", "pl", "48", "pl.png", "POLAND"),
			new CountryCode("Portugal", "pt", "351", "pt.png", "PORTUGAL"),
			new CountryCode("Puerto Rico", "pr", "1", "pr.png", "PUERTO_RICO"),
			new CountryCode("Qatar", "qa", "974", "qa.png", "QATAR"),
			new CountryCode("Republic of the Congo", "cg", "242", "cg.png", "REPUBLIC_OF_THE_CONGO"),
			new CountryCode("Romania", "ro", "40", "ro.png", "ROMANIA"),
			new CountryCode("Russia", "ru", "7", "ru.png", "RUSSIA"),
			new CountryCode("Rwanda", "rw", "250", "rw.png", "RWANDA"),
			new CountryCode("Saint Barthelemy", "bl", "590", "fr.png", "SAINT_BARTHELEMY"), //apply FR (French) flag
			new CountryCode("Saint Helena", "sh", "290", "gb.png", "SAINT_HELENA"), //apply UK flag
			new CountryCode("Saint Kitts and Nevis", "kn", "1 869", "kn.png", "SAINT_KITTS_AND_NEVIS"),
			new CountryCode("Saint Lucia", "lc", "1 758", "lc.png", "SAINT_LUCIA"),
			new CountryCode("Saint Martin", "mf", "1 599", "fr.png", "SAINT_MARTIN"), //apply FR (French) flag
			new CountryCode("Saint Pierre and Miquelon", "pm", "508", "fr.png", "SAINT_PIERRE_AND_MIQUELON"), //apply FR (French) flag
			new CountryCode("Saint Vincent and the Grenadines", "vc", "1 784", "vc.png", "SAINT_VINCENT_AND_THE_GRENADINES"),
			new CountryCode("Samoa", "ws", "685", "ws.png", "SAMOA"),
			new CountryCode("San Marino", "sm", "378", "sm.png", "SAN_MARINO"),
			new CountryCode("Sao Tome and Principe", "st", "239", "st.png", "SAO_TOME_AND_PRINCIPE"),
			new CountryCode("Saudi Arabia", "sa", "966", "sa.png", "SAUDI_ARABIA"),
			new CountryCode("Senegal", "sn", "221", "sn.png", "SENEGAL"),
			new CountryCode("Serbia", "rs", "381", "rs.png", "SERBIA"),
			new CountryCode("Seychelles", "sc", "248", "sc.png", "SEYCHELLES"),
			new CountryCode("Sierra Leone", "sl", "232", "sl.png", "SIERRA_LEONE"),
			new CountryCode("Singapore", "sg", "65", "sg.png", "SINGAPORE"),
			new CountryCode("Slovakia", "sk", "421", "sk.png", "SLOVAKIA"),
			new CountryCode("Slovenia", "si", "386", "si.png", "SLOVENIA"),
			new CountryCode("Solomon Islands", "sb", "677", "sb.png", "SOLOMON_ISLANDS"),
			new CountryCode("Somalia", "so", "252", "so.png", "SOMALIA"),
			new CountryCode("South Africa", "za", "27", "za.png", "SOUTH_AFRICA"),
			new CountryCode("South Korea", "kr", "82", "kr.png", "SOUTH_KOREA"),
			new CountryCode("Spain", "es", "34", "es.png", "SPAIN"),
			new CountryCode("Sri Lanka", "lk", "94", "lk.png", "SRI_LANKA"),
			new CountryCode("Sudan", "sd", "249", "sd.png", "SUDAN"),
			new CountryCode("Suriname", "sr", "597", "sr.png", "SURINAME"),
			new CountryCode("Svalbard", "sj", "47", "no.png", "SVALBARD"), //apply NO (Norway) flag
			new CountryCode("Swaziland", "sz", "268", "sz.png", "SWAZILAND"),
			new CountryCode("Sweden", "se", "46", "se.png", "SWEDEN"),
			new CountryCode("Switzerland", "ch", "41", "ch.png", "SWITZERLAND"),
			new CountryCode("Syria", "sy", "963", "sy.png", "SYRIA"),
			new CountryCode("Taiwan", "tw", "886", "tw.png", "TAIWAN"),
			new CountryCode("Tajikistan", "tj", "992", "tj.png", "TAJIKISTAN"),
			new CountryCode("Tanzania", "tz", "255", "tz.png", "TANZANIA"),
			new CountryCode("Thailand", "th", "66", "th.png", "THAILAND"),
			new CountryCode("Timor-Leste", "tl", "670", "tl.png", "TIMOR_LESTE"),
			new CountryCode("Togo", "tg", "228", "tg.png", "TOGO"),
			new CountryCode("Tokelau", "tk", "690", "nz.png", "TOKELAU"), //apply NZ (New Zealand) flag
			new CountryCode("Tonga", "to", "676", "to.png", "TONGA"),
			new CountryCode("Trinidad and Tobago", "tt", "1 868", "tt.png", "TRINIDAD_AND_TOBAGO"),
			new CountryCode("Tunisia", "tn", "216", "tn.png", "TUNISIA"),
			new CountryCode("Turkey", "tr", "90", "tr.png", "TURKEY"),
			new CountryCode("Turkmenistan", "tm", "993", "tm.png", "TURKMENISTAN"),
			new CountryCode("Turks and Caicos Islands", "tc", "1 649", "gb.png", "TURKS_AND_CAICOS_ISLANDS"), //apply UK flag
			new CountryCode("Tuvalu", "tv", "688", "tv.png", "TUVALU"),
			new CountryCode("Uganda", "ug", "256", "ug.png", "UGANDA"),
			new CountryCode("Ukraine", "ua", "380", "ua.png", "UKRAINE"),
			new CountryCode("United Arab Emirates", "ae", "971", "ae.png", "UNITED_ARAB_EMIRATES"),
			new CountryCode("United Kingdom", "gb", "44", "gb.png", "UNITED_KINGDOM"),
			new CountryCode("United States", "us", "1", "us.png", "UNITED_STATES"),
			new CountryCode("Uruguay", "uy", "598", "uy.png", "URUGUAY"),
			new CountryCode("US Virgin Islands", "vi", "1 340", "vi.png", "US_VIRGIN_ISLANDS"),
			new CountryCode("Uzbekistan", "uz", "998", "uz.png", "UZBEKISTAN"),
			new CountryCode("Vanuatu", "vu", "678", "vu.png", "VANUATU"),
			new CountryCode("Venezuela", "ve", "58", "ve.png", "VENEZUELA"),
			new CountryCode("Vietnam", "vn", "84", "vn.png", "VIETNAM"),
			new CountryCode("Wallis and Futuna", "wf", "681", "fr.png", "WALLIS_AND_FUTUNA"), //apply FR (French) flag
			//Not sure what to do here.  It is occupied by two countries since 1975 and prior to that it was occupied by Spain since the 19th century.  
			//They don't seem to have a flag.  Perhaps the UN flag?
			//new CountryCode("Western Sahara", "eh", "212", "eh.png", ""), //same dialing code as morrocco?
			new CountryCode("Yemen", "ye", "967", "ye.png", "YEMEN"),
			new CountryCode("Zambia", "zm", "260", "zm.png", "ZAMBIA"),
			new CountryCode("Zimbabwe", "zw", "263", "zw.png", "ZIMBABWE")
		};

		public static CountryCode getCountryFromCode(string code) {
			CountryCode retVal = null;

			if (countryMapByCode == null) {
				countryMapByCode = new Dictionary<string, CountryCode> ();
				foreach(CountryCode c in countries) {
					countryMapByCode.Add (c.countryCode, c);
				}
			}

			if(countryMapByCode.ContainsKey(code)) {
				countryMapByCode.TryGetValue(code, out retVal);
			}

			return retVal;
		}

		public static CountryCode getCountryFromName(string name) {
			CountryCode retVal = null;

			if (countryMapByName == null) {
				countryMapByName = new Dictionary<string, CountryCode> ();
				foreach(CountryCode c in countries) {
					countryMapByName.Add (c.countryName, c);
				}
			}

			if(countryMapByName.ContainsKey(name)) {
				countryMapByName.TryGetValue(name, out retVal);
			}

			return retVal;
		}

		public static CountryCode getCountryFromPosition(int position) {
			CountryCode retVal = null;

			if (position >= 0) {
				var ccEnum = CountryCode.countries.GetEnumerator ();
				int index = 0;
				while (ccEnum.MoveNext ()) {
					if (index == position)
						return ccEnum.Current;

					index++;
				}
			}

			return retVal;
		}

		public static int getIndexFromCountryCode(CountryCode cc) {
			if (cc != null) {
				var ccEnum = CountryCode.countries.GetEnumerator ();
				var index = 0;
				while (ccEnum.MoveNext ()) {
					if (cc.countryName.Equals (ccEnum.Current.countryName))
						return index;

					index++;
				}
			}

			return -1;
		}
	}
}