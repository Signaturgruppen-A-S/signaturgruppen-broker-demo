var app = new Vue({
    el: '#app',
    data: {
        baseUrl: '/Home/LoggedInSuccess?',
        popupActivated: false,
        wref: {},
        debugMode: false,
        videoFlow: false,
        activeVideo: 0,
        videoList: [
            {
                name: 'MitID password login',
                mitidLogo: true,
                nemidLogo: false,
                src: 'mitid-login.mp4'
            },
            {
                name: 'MitID code login',
                mitidLogo: true,
                nemidLogo: false,
                src: 'mitid-code.mp4'
            },
            {
                name: 'MitID with validation of social security number',
                mitidLogo: true,
                nemidLogo: false,
                src: 'mitid-ssn.mp4'
            },
            {
                name: 'MitID with transaction signing',
                mitidLogo: true,
                nemidLogo: false,
                src: 'mitid-transaction.mp4'
            },
            {
                name: 'MitID with transaction signing on mobile',
                mitidLogo: true,
                nemidLogo: false,
                src: 'mitid-transaction-mobile.mp4'
            },
            {
                name: 'NemID login',
                mitidLogo: true,
                nemidLogo: false,
                src: 'nemid-login.mp4'
            },
            {
                name: 'NemID with transaction signing',
                mitidLogo: false,
                nemidLogo: true,
                src: 'nemid-transaction.mp4'
            },
            {
                name: 'Splashscreen with both NemId and MitID',
                mitidLogo: true,
                nemidLogo: true,
                src: 'splashscreen.mp4'
            }
        ],
        persistedParameters: {
            landingpageVersion: '1.1',
            advancedOptions: false,
            language: 'da-DK',
            idp: {
                mitid: true,
                nemid: true
            },
            mitidSpecific: {
                reference_text: '',
                transactionSigning: false,
                transaction_sign_type: 'html',
                transaction_sign_content_html: '<html>\r\n <style>\r\n  ul{\r\n      list-style: none;\r\n      margin: 0;\r\n      padding: 1rem;\r\n  }\r\n  ul li{\r\n      display: flex;\r\n      flex-direction: column;\r\n      margin-bottom: 2rem;\r\n  }\r\n\r\n  .key{\r\n      color: #adb5bd;\r\n      font-size: 90%;\r\n      margin-bottom: 5px;\r\n      text-transform: uppercase;\r\n  }\r\n  .value{\r\n      font-weight: bold;\r\n  }\r\n <\/style>\r\n <body>\r\n  <ul>\r\n   <li>\r\n    <div class=\"key\">From Account<\/div>\r\n    <div class=\"value\">Salary (51 3184 8481)<\/div>\r\n   <\/li>\r\n   <li>\r\n    <div class=\"key\">Reciever<\/div>\r\n    <div class=\"value\">EON (1551 458484448)<\/div>\r\n   <\/li>\r\n   <li>\r\n    <div class=\"key\">Reference<\/div>\r\n    <div class=\"value\">Invoice #15418144<\/div>\r\n   <\/li>\r\n   <li>\r\n    <div class=\"key\">Date<\/div>\r\n    <div class=\"value\">24 February 2020<\/div>\r\n   <\/li>\r\n   <li>\r\n    <div class=\"key\">Amount<\/div>\r\n    <div class=\"value total\">18.484,05<small> DKK<\/small><\/div>\r\n   <\/li>\r\n  <\/ul>\r\n <\/body>\r\n<\/html>',
                transaction_sign_content_plain: 'Type your sign text here',
                require_psd2: false,
                nemid_pid: false,
                loa_value: 'https://data.gov.dk/concept/core/nsis/Substantial',
                enable_step_up: false
            },
            nemidSpecific: {
                apptransactiontext: '',
                nemid_private_to_business: false,
                transactionSigning: false,
                transaction_sign_type: '',
                transaction_sign_content_plain: 'Type your sign text here'
            },
            uxType: 'redirect',
            ssn: false,
            transactionToken: false,
            userinfoToken: false
        }
    },
    computed: {
        queryBuilder: function () {
            let paramsValues = {}
            let scope = []
            let acr = []
            if (this.persistedParameters.mitidSpecific.transactionSigning) {
                paramsValues.mitid_sign_text_type = this.persistedParameters.mitidSpecific.transaction_sign_type
                if (paramsValues.mitid_sign_text_type === 'html') {
                    paramsValues.mitid_sign_text = this.b64(this.persistedParameters.mitidSpecific.transaction_sign_content_html)
                } else {
                    paramsValues.mitid_sign_text = this.b64(this.persistedParameters.mitidSpecific.transaction_sign_content_plain)
                }
            }
            if (this.persistedParameters.nemidSpecific.transactionSigning) {
                paramsValues.nemid_sign_text_type = this.persistedParameters.nemidSpecific.transaction_sign_type
                if (paramsValues.nemid_sign_text_type === 'text') {
                    paramsValues.nemid_sign_text = this.b64(this.persistedParameters.nemidSpecific.transaction_sign_content_plain)
                } else {
                    paramsValues.nemid_signpdf = true
                }
            }
            paramsValues.mitidEnabled = this.persistedParameters.idp.mitid
            paramsValues.nemidEnabled = this.persistedParameters.idp.nemid
            paramsValues.language = this.persistedParameters.language
            if (paramsValues.nemidEnabled) {
                scope.push('nemid')
                acr = acr.concat(this.persistedParameters.nemidSpecific.acr)
                paramsValues.nemid_private_to_business = this.persistedParameters.nemidSpecific.nemid_private_to_business
            }
            if (paramsValues.mitidEnabled) {
                scope.push('mitid')
            }
            if (acr.length > 0) {
                paramsValues.acr = acr.join(' ')
            }
            if (this.persistedParameters.ssn) {
                scope.push('ssn')
            }
            if (this.persistedParameters.transactionToken) {
                scope.push('transaction_token')
            }
            if (this.persistedParameters.userinfoToken) {
                scope.push('userinfo_token')
            }
            if (scope.length > 0) {
                paramsValues.scope = scope.join(' ')
            }
            if (this.persistedParameters.mitidSpecific.reference_text) {
                paramsValues.mitid_reference_text = this.b64(this.persistedParameters.mitidSpecific.reference_text)
            }
            if (this.persistedParameters.mitidSpecific.require_psd2) {
                paramsValues.mitid_require_psd2 = this.persistedParameters.mitidSpecific.require_psd2
            }
            if (this.persistedParameters.mitidSpecific.nemid_pid) {
                paramsValues.nemid_pid = this.persistedParameters.mitidSpecific.nemid_pid
            }
            if (this.persistedParameters.mitidSpecific.loa_value) {
                paramsValues.mitid_loa_value = this.persistedParameters.mitidSpecific.loa_value
            }
            if (this.persistedParameters.nemidSpecific.apptransactiontext) {
                paramsValues.nemid_apptransactiontext = this.b64(this.persistedParameters.nemidSpecific.apptransactiontext)
            }
            let paramsValuesEncoded = ''
            for (var prop in paramsValues) {
                if (paramsValues.hasOwnProperty(prop)) {
                    var k = encodeURIComponent(prop),
                        v = encodeURIComponent(paramsValues[prop])
                    paramsValuesEncoded = paramsValuesEncoded + k + "=" + v + '&'
                }
            }
            return paramsValuesEncoded.slice(0, -1)
        },
        formValid: function() {
            return this.idpSelected && this.mitIdValid && this.uxSelected;
        },
        mitIdValid: function () {
            return this.persistedParameters.idp.mitid ? this.persistedParameters.mitidSpecific.loa_value : true;
        },
        uxSelected: function () {
            return !(this.persistedParameters.uxType === '');
        },
        idpSelected: function () {
            return (this.persistedParameters.idp.mitid || this.persistedParameters.idp.nemid)
        }
    },
    methods: {
        activateVideo: function (index) {
            this.activeVideo = index
            if (this.videoList[this.activeVideo].src) {
                this.$refs.videoRef.src = "/video/" + this.videoList[this.activeVideo].src;
                if(this.videoFlow){
                    this.$refs.videoRef.play();
                }
            }
        },
        b64: function (content) {
            return btoa(unescape(encodeURIComponent(content)))
        },
        toggleAdvanced: function() {
            this.persistedParameters.advancedOptions = !this.persistedParameters.advancedOptions
        },
        toggleVideo: function () {
            this.videoFlow = !this.videoFlow;
            this.$refs.videoRef.play();
        },
        basicSignin: function() {
            window.location.href = this.baseUrl
        },
        callOP: function(event) {
            if (event !== undefined) {
                event.preventDefault()
            }
            if(Object.keys(this.wref).length !== 0 && !this.wref.closed){
                this.onFocus()
            } else {
                if (this.persistedParameters.uxType === 'redirect') {
                    if (event !== undefined) {
                        event.target.reset()
                    }
                    window.location.href = this.baseUrl + this.queryBuilder
                }else if(this.persistedParameters.uxType==='popup'){
                    if (this.persistedParameters.mitidSpecific.transactionSigning || this.persistedParameters.nemidSpecific.transactionSigning){
                        this.popupwindow(this.baseUrl+this.queryBuilder, 'Sign in','878','640')
                    } else {
                        let idpCount = 0;
                        for (let k in this.persistedParameters.idp) {
                            if (this.persistedParameters.idp[k]) {
                                idpCount++
                            }
                        }

                        if (idpCount == 1) {
                            this.popupwindow(this.baseUrl + this.queryBuilder, 'Sign in', '452', '640')
                        } else {
                            this.popupwindow(this.baseUrl + this.queryBuilder, 'Sign in', '452', '692')
                        }
                    }
                }
            }
        },
        popupwindow: function(n, t, i, r){
            this.popupActivated = true
            let u = window.outerHeight / 2 + window.screenY - r / 2
            let f = window.outerWidth / 2 + window.screenX - i / 2
            this.wref = window.open(n, t, 'toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no, resizable=no, copyhistory=no, width=' + i + ', height=' + r + ', top=' + u + ', left=' + f)
            this.wref.focus()
        },
        onFocus: function(){
            if(Object.keys(this.wref).length !== 0){
                setTimeout(function () { app.wref.focus() }, 100)
            }
        },
        closeWindow: function(){
            if(Object.keys(this.wref).length !== 0){
                this.wref.close()
                this.popupActivated = false
                this.wref = {}
            }
            this.popupActivated = false
        },
        receiveMessage: function(event){
            if (event.data === 'CLOSE') {
                this.closeWindow()
            } else if (event.data === 'SUCCESS') {
                this.closeWindow()
                window.location.href = '/Secure/Claims'
            }
        }
    },
    mounted: function () {
        if (this.videoList.length > 0) {
            this.activateVideo(0)
        }
    },
    created: function () {
        window.addEventListener("message", this.receiveMessage, false)
        window.addEventListener("focus", this.onFocus)
        if (location.hostname === "localhost") {
            this.debugMode = true
        }
    }
})