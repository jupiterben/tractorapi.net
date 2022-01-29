import os
import socket
import getpass
import tractor.api.author as author

home = os.path.expanduser("~")
app = os.path.basename(__file__)
user = "tractor"
host = socket.gethostname()
# qualify the session filename by host if it will be stored in a location shared my multiple hosts
sessionFilename = os.path.join(home, ".{app}.{user}.{host}.session".format(
    home=home, app=app, user=user, host=host))
# set the session filename
author.setEngineClientParam(hostname="sh.nearhub.com",
                        port=1234, user="tractor")
author.setEngineClientParam(sessionFilename=sessionFilename)


job = author.Job(title="animFile gen job",
                 priority=100, service="PixarRender")
job.newTask(title="create file",
            argv=["/usr/bin/prman", "file.rib"], service="pixarRender")
print(job.asTcl())
newJid = job.spool()
print(newJid)
