
pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/akshat180shah/Meccha-Chameleon-AR-SLA.git',
                    branch: 'main'
            }
        }

        stage('Verify Unity Project') {
            steps {
                bat 'if not exist Assets exit /b 1'
                bat 'dir Assets'
                bat 'echo Unity project Assets folder found.'
            }
        }
    }
}
