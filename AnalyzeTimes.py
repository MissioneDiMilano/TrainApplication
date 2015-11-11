import re

def analyze(fIn, fOut, hours, minutes):

	fileIn = open(fIn,"r")
	fileOut = open(fOut, "w")

	fileContent = fileIn.read()
	result = re.findall("[\w\s]+- \d+:\d+",fileContent, re.UNICODE)
	resultStripped = []

	for r in result:
		resultStripped.append(r.strip())

	resultCloseEnough = []
	resultTooFar = []
	resultNothing = []

	for r in resultStripped:
		pts = r[-4:].split(":")
		one = int(pts[0])
		two = int(pts[1])
		if (one+two)==0:
			resultNothing.append(r)
		else:
			if (one == hours and two < minutes):
				resultCloseEnough.append(r)
			elif (one > 0):
				resultTooFar.append(r)
			else:
				resultCloseEnough.append(r)


	coppie = 0

	if len(resultCloseEnough) == 0:
		fileOut.write("Nothing is closer than "+str(hours)+ " hour(s) "+ str(minutes) + " minute(2)")
	for r in resultCloseEnough:
		coppie += 1
		fileOut.write(r)
		fileOut.write("\n")
	fileOut.write("\n\n\n")
	for r in resultTooFar:
		coppie += 1
		fileOut.write(r)
		fileOut.write("\n")

	fileOut.write("\n\n\n")
	for r in resultNothing:
		coppie += 1
		fileOut.write(r)
		fileOut.write("\n")
	print str(coppie)+" coppie."
