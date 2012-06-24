#!/usr/bin/python

"""
"""

import sys
import os
import getopt
import smtplib

import cgi
import cgitb; cgitb.enable()

SMTP_HOST = "174.132.190.194"
SMTP_USER = "robots@uvoutliner.com"
SMTP_PASSWD = ""

FAILED_RESULT = 'failed.'
SUCCESS_RESULT = 'done.'

def main():

    form = cgi.FieldStorage()
    
    mail_from = "Error Report Processing <robots@uvoutliner.com>"
    mail_to = "fedir@uvoutliner.com"
    message = "From: %s\nTo: %s\nSubject: UV Outliner unhandled exception\n\n %s" % (mail_from, mail_to, form.getfirst("err", ""))
    
    print "Content-Type: text/html"
    print
    
    if mail_from == "" or mail_to == "" or message == "":
        sys.stdout.write(FAILED_RESULT)
        return
    
    # Now send the message
    s = smtplib.SMTP()
    s.connect(SMTP_HOST)
    s.login(SMTP_USER, SMTP_PASSWD)
    try:
        s.sendmail(mail_from, mail_to, message) 
    except:
        sys.stdout.write(FAILED_RESULT)
        print "Server exception"
        raise
    else:        
        sys.stdout.write(SUCCESS_RESULT)
        s.close()

main()
