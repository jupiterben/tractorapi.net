import os
import socket
import getpass
import tractor.api.query as tq

home = os.path.expanduser("~")
app = os.path.basename(__file__)
user = "tractor"
host = socket.gethostname()
# qualify the session filename by host if it will be stored in a location shared my multiple hosts
sessionFilename = os.path.join(home, ".{app}.{user}.{host}.session".format(
    home=home, app=app, user=user, host=host))
# set the session filename
tq.setEngineClientParam(hostname="sh.nearhub.com",
                        port=1234, user="tractor")
tq.setEngineClientParam(sessionFilename=sessionFilename)
# check if the password needs to be obtained; this block will be skipped if the session filen contains a valid session id
if tq.needsPassword():
    password = getpass.getpass()
    tq.setEngineClientParam(user=user, password=password)
 # the session file will be created when the first successful API call is made
jobs = tq.jobs("title like animFile")
print("There are {numjobs} jobs with errors.".format(numjobs=len(jobs)))
