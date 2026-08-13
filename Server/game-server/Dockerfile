# game-server Dockerfile — JRE 运行镜像
# Build context: Server/ (父目录)
# 前置条件: 在 Windows 上用 Maven 编译好 JAR，放到 game-server/target/ 目录

FROM eclipse-temurin:21-jre-jammy

WORKDIR /app
COPY game-server/target/*.jar app.jar

EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
